//
// Copyright (c) 2019- yutopp (yutopp@gmail.com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at  https://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;

namespace VJson.Schema
{
    public class JsonSchemaValidator
    {
        JsonSchema _schema;

        public JsonSchemaValidator(JsonSchema j)
        {
            _schema = j;
        }

        public ConstraintsViolationException Validate(object o, JsonSchemaRegistory reg = null)
        {
            return Validate(o, new State(), reg);
        }

        internal ConstraintsViolationException Validate(object o, State state, JsonSchemaRegistory reg)
        {
            if (_schema.Ref != null)
            {
                if (reg == null)
                {
                    reg = JsonSchemaRegistory.GetDefault();
                }
                var schema = reg.Resolve(_schema.Ref);
                if (schema == null)
                {
                    // TODO:
                    throw new NotSupportedException();
                }
                return schema.Validate(o, state, reg);
            }

            ConstraintsViolationException ex = null;

            var kind = Node.KindOfValue(o);

            if (_schema.Type != null)
            {
                if (_schema.Type.GetType().IsArray)
                {
                    var ts = (string[])_schema.Type;
                    var found = false;
                    foreach (var t in ts)
                    {
                        if (ValidateKind(kind, t))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var actual = kind.ToString();
                        var expected = String.Join(", ", ts);
                        var msg = state.CreateMessage("Type is not contained(Actual: {0}; Expected: [{1}])",
                                                      actual, expected);
                        return new ConstraintsViolationException(msg);
                    }

                }
                else
                {
                    var t = (string)_schema.Type;
                    if (!ValidateKind(kind, t))
                    {
                        var actual = kind.ToString();
                        var expected = t.ToString();
                        var msg = state.CreateMessage("Type is not matched(Actual: {0}; Expected: {1})",
                                                      actual, expected);
                        return new ConstraintsViolationException(msg);
                    }
                }
            }

            if (_schema.Enum != null)
            {
                var found = false;
                foreach (var e in _schema.Enum)
                {
                    if (TypeHelper.DeepEquals(o, e))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var msg = state.CreateMessage("Enum is not matched");
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.Not != null)
            {
                ex = _schema.Not.Validate(o, state, reg);
                if (ex == null)
                {
                    return new ConstraintsViolationException("Not", ex);
                }
            }

            switch (kind)
            {
                case NodeKind.Boolean:
                    break;

                case NodeKind.Float:
                case NodeKind.Integer:
                    ex = ValidateNumber(Convert.ToDouble(o), state, reg);

                    if (ex != null)
                    {
                        return new ConstraintsViolationException("Number", ex);
                    }
                    break;

                case NodeKind.String:
                    ex = ValidateString((string)o, state, reg);
                    if (ex != null)
                    {
                        return new ConstraintsViolationException("String", ex);
                    }
                    break;

                case NodeKind.Array:
                    ex = ValidateArray(TypeHelper.ToIEnumerable(o), state, reg);
                    if (ex != null)
                    {
                        return new ConstraintsViolationException("Array", ex);
                    }
                    break;

                case NodeKind.Object:
                    ex = ValidateObject(o, state, reg);
                    if (ex != null)
                    {
                        return new ConstraintsViolationException("Object", ex);
                    }
                    break;

                case NodeKind.Null:
                    break;

                default:
                    throw new NotImplementedException(kind.ToString());
            }

            return null;
        }

        ConstraintsViolationException ValidateNumber(double v, State state, JsonSchemaRegistory reg)
        {
            if (_schema.MultipleOf != double.MinValue)
            {
                throw new NotImplementedException();
            }

            if (_schema.Maximum != double.MinValue)
            {
                if (!(v <= _schema.Maximum))
                {
                    var msg = state.CreateMessage("Maximum assertion !({0} <= {1})",
                                                  v, _schema.Maximum);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.ExclusiveMaximum != double.MinValue)
            {
                if (!(v < _schema.ExclusiveMaximum))
                {
                    var msg = state.CreateMessage("ExclusiveMaximum assertion !({0} < {1})",
                                                  v, _schema.ExclusiveMaximum);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.Minimum != double.MaxValue)
            {
                if (!(v >= _schema.Minimum))
                {
                    var msg = state.CreateMessage("Minimum assertion !({0} >= {1})",
                                                  v, _schema.Minimum);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.ExclusiveMinimum != double.MaxValue)
            {
                if (!(v > _schema.ExclusiveMinimum))
                {
                    var msg = state.CreateMessage("ExclusiveMinimum assertion !({0} > {1})",
                                                  v, _schema.ExclusiveMinimum);
                    return new ConstraintsViolationException(msg);
                }
            }

            return null;
        }

        ConstraintsViolationException ValidateString(string v, State state, JsonSchemaRegistory reg)
        {
            StringInfo si = null;

            if (_schema.MaxLength != int.MinValue)
            {
                si = si ?? new StringInfo(v);
                if (!(si.LengthInTextElements <= _schema.MaxLength))
                {
                    var msg = state.CreateMessage("MaxLength assertion !({0} <= {1})",
                                                  si.LengthInTextElements, _schema.MaxLength);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.MinLength != int.MaxValue)
            {
                si = si ?? new StringInfo(v);
                if (!(si.LengthInTextElements >= _schema.MinLength))
                {
                    var msg = state.CreateMessage("MinLength assertion !({0} >= {1})",
                                                  si.LengthInTextElements, _schema.MinLength);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.Pattern != null)
            {
                if (!Regex.IsMatch(v, _schema.Pattern))
                {
                    var msg = state.CreateMessage("Pattern assertion !(\"{0}\" matched \"{1}\")",
                                                  v, _schema.Pattern);
                    return new ConstraintsViolationException(msg);
                }
            }

            return null;
        }

        ConstraintsViolationException ValidateArray(IEnumerable<object> v, State state, JsonSchemaRegistory reg)
        {
            var length = v.Count();

            if (_schema.MaxItems != int.MinValue)
            {
                if (!(length <= _schema.MaxItems))
                {
                    var msg = state.CreateMessage("MaxItems assertion !({0} <= {1})",
                                                  length, _schema.MaxItems);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.MinItems != int.MaxValue)
            {
                if (!(length >= _schema.MinItems))
                {
                    var msg = state.CreateMessage("MinItems assertion !({0} >= {1})",
                                                  length, _schema.MinItems);
                    return new ConstraintsViolationException(msg);
                }
            }

            List<object> extraItems = null;

            if (_schema.Items != null)
            {
                if (_schema.Items.GetType().IsArray)
                {
                    var itemSchemas = (JsonSchema[])_schema.Items;

                    var i = 0;
                    foreach (var elem in v)
                    {
                        var itemSchema = itemSchemas.ElementAtOrDefault(i);
                        if (itemSchema == null)
                        {
                            if (extraItems == null)
                            {
                                extraItems = new List<object>();
                            }
                            extraItems.Add(elem);
                            continue;
                        }

                        var ex = itemSchema.Validate(elem, state.NestAsElem(i), reg);
                        if (ex != null)
                        {
                            return new ConstraintsViolationException("Items", ex);
                        }

                        ++i;
                    }

                }
                else
                {
                    var itemSchema = (JsonSchema)_schema.Items;
                    var i = 0;
                    foreach (var elem in v)
                    {
                        var ex = itemSchema.Validate(elem, state.NestAsElem(i), reg);
                        if (ex != null)
                        {
                            return new ConstraintsViolationException("Items", ex);
                        }

                        ++i;
                    }
                }
            }

            if (_schema.AdditionalItems != null)
            {
                if (extraItems != null)
                {
                    foreach (var elem in extraItems)
                    {
                        var ex = _schema.AdditionalItems.Validate(elem, state, reg);
                        if (ex != null)
                        {
                            return new ConstraintsViolationException("AdditionalItems", ex);
                        }
                    }
                }
            }

            return null;
        }

        ConstraintsViolationException ValidateObject(object v, State state, JsonSchemaRegistory reg)
        {
            var validated = new Dictionary<string, object>();

            foreach (var kv in TypeHelper.ToKeyValues(v))
            {
                var ex = ValidateObjectField(kv.Key, kv.Value, state.NestAsElem(kv.Key), reg);
                if (ex != null)
                {
                    return ex;
                }

                validated.Add(kv.Key, kv.Value);
            }

            if (_schema.Required != null)
            {
                var req = new HashSet<string>(_schema.Required);
                req.IntersectWith(validated.Keys);

                if (req.Count != _schema.Required.Count())
                {
                    var actual = String.Join(", ", req.ToArray());
                    var expected = String.Join(", ", _schema.Required);
                    var msg = state.CreateMessage("Lack of required fields(Actual: [{0}]; Expected: [{1}])",
                                                  actual, expected);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.MaxProperties != int.MinValue)
            {
                if (!(validated.Count <= _schema.MaxProperties))
                {
                    var msg = state.CreateMessage("MaxProperties assertion !({0} <= {1})",
                                                  validated.Count, _schema.MaxProperties);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.MinProperties != int.MaxValue)
            {
                if (!(validated.Count >= _schema.MinProperties))
                {
                    var msg = state.CreateMessage("MaxProperties assertion !({0} >= {1})",
                                                  validated.Count, _schema.MinProperties);
                    return new ConstraintsViolationException(msg);
                }
            }

            if (_schema.Dependencies != null)
            {
                var strDep = _schema.Dependencies as Dictionary<string, string[]>;
                if (strDep != null)
                {
                    foreach (var va in validated)
                    {
                        string[] deps = null;
                        if (strDep.TryGetValue(va.Key, out deps))
                        {
                            var intersected = ((string[])deps.Clone()).Intersect(validated.Keys);
                            if (intersected.Count() != deps.Count())
                            {
                                var actual = String.Join(", ", intersected.ToArray());
                                var expected = String.Join(", ", deps);
                                var msg = state.CreateMessage("Dependencies assertion. Lack of depended fields for {0}(Actual: [{1}]; Expected: [{2}])",
                                                              va.Key, actual, expected);
                                return new ConstraintsViolationException(msg);
                            }
                        }
                    }
                    goto depChecked;
                }

                var schemaDep = _schema.Dependencies as Dictionary<string, JsonSchema>;
                if (schemaDep != null)
                {
                    foreach (var va in validated)
                    {
                        JsonSchema ext = null;
                        if (schemaDep.TryGetValue(va.Key, out ext))
                        {
                            var ex = ext.Validate(v, new State().NestAsElem(va.Key), reg);
                            if (ex != null)
                            {
                                // TODO:
                                var msg = state.CreateMessage("Dependencies assertion. Failed to validation for {0}",
                                                              va.Key);
                                return new ConstraintsViolationException(msg, ex);
                            }
                        }
                    }
                }

            depChecked:
                ;
            }

            return null;
        }

        ConstraintsViolationException ValidateObjectField(string key,
                                                          object value,
                                                          State state,
                                                          JsonSchemaRegistory reg)
        {
            var matched = false;

            if (_schema.Properties != null)
            {
                JsonSchema itemSchema = null;
                if (_schema.Properties.TryGetValue(key, out itemSchema))
                {
                    matched = true;

                    var ex = itemSchema.Validate(value, state, reg);
                    if (ex != null)
                    {
                        return new ConstraintsViolationException("Property", ex);
                    }
                }
            }

            if (_schema.PatternProperties != null)
            {
                foreach (var pprop in _schema.PatternProperties)
                {
                    if (Regex.IsMatch(key, pprop.Key))
                    {
                        matched = true;

                        var ex = pprop.Value.Validate(value, state, reg);
                        if (ex != null)
                        {
                            return new ConstraintsViolationException("PatternProperties", ex);
                        }
                    }
                }
            }

            if (_schema.AdditionalProperties != null && !matched)
            {
                var ex = _schema.AdditionalProperties.Validate(value, state, reg);
                if (ex != null)
                {
                    return new ConstraintsViolationException("AdditionalProperties", ex);
                }
            }

            return null;
        }

        /// <summary>
        ///   true if valid
        /// </summary>
        static bool ValidateKind(NodeKind kind, string typeName)
        {
            switch (typeName)
            {
                case "null":
                    return kind == NodeKind.Null;

                case "boolean":
                    return kind == NodeKind.Boolean;

                case "object":
                    return kind == NodeKind.Object;

                case "array":
                    return kind == NodeKind.Array;

                case "number":
                    return kind == NodeKind.Integer || kind == NodeKind.Float;

                case "string":
                    return kind == NodeKind.String;

                case "integer":
                    return kind == NodeKind.Integer;

                default:
                    throw new NotImplementedException();
            }
        }

        internal struct State
        {
            string _elemName;

            internal State NestAsElem(int elem)
            {
                return new State()
                {
                    _elemName = String.Format("{0}[{1}]", _elemName, elem),
                };
            }

            internal State NestAsElem(string elem)
            {
                return new State()
                {
                    _elemName = String.Format("{0}[\"{1}\"]", _elemName, elem),
                };
            }

            internal string CreateMessage(string format, params object[] args)
            {
                return String.Format("{0}: {1}.", _elemName, String.Format(format, args));
            }
        }
    }

    public class ConstraintsViolationException : Exception
    {
        public ConstraintsViolationException(string message)
            : base(message)
        {
        }

        public ConstraintsViolationException(string message, ConstraintsViolationException inner)
            : base(message, inner)
        {
        }

        public string Diagnosis()
        {
            return DiagnosisAcc(new List<string>() { Message });
        }

        string DiagnosisAcc(List<string> acc)
        {
            if (InnerException != null)
            {
                var inner = (ConstraintsViolationException)InnerException;
                acc.Add(inner.Message);
                return inner.DiagnosisAcc(acc);
            }
            return String.Join(".", acc.ToArray());
        }
    }
}