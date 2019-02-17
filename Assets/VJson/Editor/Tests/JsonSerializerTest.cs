//
// Copyright (c) 2019- yutopp (yutopp@gmail.com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at  https://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace VJson.UnitTests
{
    class SomeObject
    {
        private float _p = 3.14f; // A private field will not be exported.
        public int X;
        public string Y;

        public override bool Equals(object rhsObj)
        {
            var rhs = rhsObj as SomeObject;
            if (rhs == null)
            {
                return false;
            }

            return _p == rhs._p && X == rhs.X && Y == rhs.Y;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    class DerivedSomeObject : SomeObject
    {
        public bool D;

        public override bool Equals(object rhsObj)
        {
            var rhs = rhsObj as DerivedSomeObject;
            if (rhs == null)
            {
                return false;
            }

            return D == rhs.D && base.Equals(rhsObj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    class CustomObject
    {
        [JsonField(TypeHints = new Type[] { typeof(bool), typeof(SomeObject) })]
        public object Obj = default(object);

        public override bool Equals(object rhsObj)
        {
            var rhs = rhsObj as CustomObject;
            if (rhs == null)
            {
                return false;
            }

            return object.Equals(Obj, rhs.Obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    class CustomObjectHasArray
    {
        [JsonField(TypeHints = new Type[] { typeof(SomeObject), typeof(SomeObject[]) })]
        public object Obj = default(object);

        public override bool Equals(object rhsObj)
        {
            var rhs = rhsObj as CustomObjectHasArray;
            if (rhs == null)
            {
                return false;
            }

            if (Obj == null && rhs.Obj == null)
            {
                return true;
            }

            if (Obj == null || rhs.Obj == null)
            {
                return false;
            }

            var lhsArr = Obj as SomeObject[];
            var rhsArr = rhs.Obj as SomeObject[];
            if (lhsArr != null && rhsArr != null)
            {
                return lhsArr.SequenceEqual(rhsArr);
            }

            var lhsSgt = Obj as SomeObject;
            var rhsAgt = rhs.Obj as SomeObject;
            return Object.Equals(lhsSgt, rhsAgt);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    class RenamedObject
    {
        [JsonField(Name = "renamed")]
        public int Actual;

        public override bool Equals(object rhsObj)
        {
            var rhs = rhsObj as RenamedObject;
            if (rhs == null)
            {
                return false;
            }

            return Actual == rhs.Actual;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    class IgnorableObject
    {
        [JsonFieldIgnorable(WhenValueIs = 0)]
        public int Ignore0;

        [JsonFieldIgnorable(WhenLengthIs = 0)]
        public List<int> Ignore1;

        public override bool Equals(object rhsObj)
        {
            var rhs = rhsObj as IgnorableObject;
            if (rhs == null)
            {
                return false;
            }

            return Ignore0 == rhs.Ignore0
                && ((Ignore1 == null && rhs.Ignore1 == null)
                    || Ignore1 != null && rhs.Ignore1 != null && Ignore1.SequenceEqual(rhs.Ignore1));
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    [Json(ImplicitConstructable = true)]
    class Hoge
    {
        public bool B { get; private set; }
        public Hoge(bool b)
        {
            B = b;
        }
    }

    class JsonSerializerTests
    {
        [Test]
        [TestCaseSource("CommonArgs")]
        [TestCaseSource("OnlySerializeArgs")]
        public void SerializeTest(object obj, string expected)
        {
            var serializer = new VJson.JsonSerializer(obj != null ? obj.GetType() : typeof(object));

            using (var textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, obj);
                var actual = textWriter.ToString();

                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        [TestCaseSource("CommonArgs")]
        [TestCaseSource("OnlySerializeArgs")]
        public void SerializeFromStringTest(object obj, string expected)
        {
            var serializer = new VJson.JsonSerializer(obj != null ? obj.GetType() : typeof(object));

            var actual = serializer.Serialize(obj);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCaseSource("CommonArgs")]
        [TestCaseSource("OnlyDeserializeArgs")]
        public void DeserializeTest(object obj, string expected)
        {
            var serializer = new VJson.JsonSerializer(obj != null ? obj.GetType() : typeof(object));
            var actual = serializer.Deserialize(expected);

            Assert.AreEqual(obj, actual);
        }

        [Test]
        [TestCaseSource("CommonArgs")]
        [TestCaseSource("OnlyDeserializeArgs")]
        public void DeserializeFromStringTest(object obj, string expected)
        {
            var serializer = new VJson.JsonSerializer(obj != null ? obj.GetType() : typeof(object));
            var actual = serializer.Deserialize(expected);

            Assert.AreEqual(obj, actual);
        }

        //
        static object[] CommonArgs = {
            // Boolean
            new object[] {
                true,
                @"true",
            },

            new object[] {
                false,
                @"false",
            },

            // Null
            new object[] {
                (object)null,
                @"null",
            },

            // Numbers
            new object[] {
                (byte)1,
                @"1",
            },
            new object[] {
                (short)1,
                @"1",
            },
            new object[] {
                (int)1,
                @"1",
            },
            new object[] {
                (long)1,
                @"1",
            },
            new object[] {
                (float)3.14,
                @"3.14",
            },
            new object[] {
                (double)3.14,
                @"3.14",
            },

            // Strings
            new object[] {
                "🍣",
                @"""🍣"""
            },

            // Arrays
            new object[] {
                new object[] {1, "hoge", null},
                @"[1,""hoge"",null]",
            },
            new object[] {
                new int[] {1, 2, 3},
                @"[1,2,3]",
            },
            new object[] {
                new List<object> {1, 2, 3},
                @"[1,2,3]",
            },
            new object[] {
                new List<int> {1, 2, 3},
                @"[1,2,3]",
            },

            // Objects
            new object[] {
                new SomeObject {
                    X = 10,
                    Y = "abab",
                },
                @"{""X"":10,""Y"":""abab""}",
            },
            new object[] {
                new DerivedSomeObject {
                    X = 10,
                    Y = "abab",
                    D = true,
                },
                @"{""D"":true,""X"":10,""Y"":""abab""}",
            },
            new object[] {
                (SomeObject)(new DerivedSomeObject {
                        X = 20,
                        Y = "cdcd",
                        D = false,
                    }),
                @"{""D"":false,""X"":20,""Y"":""cdcd""}",
            },
            new object[] {
                new RenamedObject {
                    Actual = 42,
                },
                @"{""renamed"":42}",
            },
            new object[] {
                new IgnorableObject {
                    Ignore0 = 0,
                },
                @"{}",
            },
            new object[] {
                new IgnorableObject {
                    Ignore0 = 1,
                },
                @"{""Ignore0"":1}",
            },
            new object[] {
                new IgnorableObject {
                    Ignore1 = new List<int> {1},
                },
                @"{""Ignore1"":[1]}",
            }
        };

        static object[] OnlySerializeArgs = {
            new object[] {
                new IgnorableObject {
                    Ignore1 = new List<int>(),
                },
                @"{}",
            },
        };

        static object[] OnlyDeserializeArgs = {
        };
    }

    class JsonSerializerForContainerTests
    {
        [Test]
        [TestCaseSource("ListArgs")]
        public void SerializeTest<E>(IEnumerable<E> obj, string expected)
        {
            var serializer = new VJson.JsonSerializer(obj != null ? obj.GetType() : typeof(object));
            using (var textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, obj);
                var actual = textWriter.ToString();

                Assert.That(actual, Is.EquivalentTo(expected));
            }
        }

        [Test]
        [TestCaseSource("DictArgs")]
        public void SerializeTest<K, V>(IDictionary<K, V> obj, string expected)
        {
            var serializer = new VJson.JsonSerializer(obj != null ? obj.GetType() : typeof(object));
            using (var textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, obj);
                var actual = textWriter.ToString();

                Assert.That(actual, Is.EquivalentTo(expected));
            }
        }

        [Test]
        [TestCaseSource("ListArgs")]
        public void DeserializeTest<E>(IEnumerable<E> expected, string json)
        {
            var serializer = new VJson.JsonSerializer(expected != null ? expected.GetType() : typeof(object));
            var actual = serializer.Deserialize(json);

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        [TestCaseSource("DictArgs")]
        public void DeserializeTest<K, V>(IDictionary<K, V> expected, string json)
        {
            var serializer = new VJson.JsonSerializer(expected != null ? expected.GetType() : typeof(object));
            var actual = serializer.Deserialize(json);

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCaseSource("ListArgs")]
        public void DeserializeUntypedTest<E>(IEnumerable<E> expected, string json)
        {
            var serializer = new VJson.JsonSerializer(typeof(object));
            var actual = serializer.Deserialize(json);

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCaseSource("DictArgs")]
        public void DeserializeUntypedTest<K, V>(IDictionary<K, V> expected, string json)
        {
            var serializer = new VJson.JsonSerializer(typeof(object));
            var actual = serializer.Deserialize(json);

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void DeserializeHasHintFieldsBoolTest()
        {
            var serializer = new VJson.JsonSerializer(typeof(CustomObject));
            var actual = (CustomObject)serializer.Deserialize(@"{""Obj"": true}");

            Assert.AreEqual(typeof(bool), actual.Obj.GetType());
            Assert.AreEqual(true, actual.Obj);
        }

        [Test]
        public void DeserializeHasHintFieldsClassTest()
        {
            var serializer = new VJson.JsonSerializer(typeof(CustomObject));
            var actual = (CustomObject)serializer.Deserialize(@"{""Obj"": {""X"":10}}");

            Assert.AreEqual(typeof(SomeObject), actual.Obj.GetType());
            Assert.AreEqual(new SomeObject
            {
                X = 10,
            }, actual.Obj);
        }

        [Test]
        public void DeserializeHasHintFieldsArrayOrSingletonSgtTest()
        {
            var serializer = new VJson.JsonSerializer(typeof(CustomObjectHasArray));
            var actual = (CustomObjectHasArray)serializer.Deserialize(@"{""Obj"": {""X"":10}}");

            Assert.AreEqual(typeof(SomeObject), actual.Obj.GetType());
            Assert.That(actual.Obj,
                        Is.EqualTo(new SomeObject
                        {
                            X = 10,
                        })
                );
        }

        [Test]
        public void DeserializeImplicitConstructableTest()
        {
            var serializer = new VJson.JsonSerializer(typeof(Hoge));
            var actual = (Hoge)serializer.Deserialize(@"true");

            Assert.That(actual.B, Is.EqualTo(true));
        }

        public static object[] ListArgs = {
            new object[] {
                new int[] {},
                @"[]",
            },
            new object[] {
                new object[] {1, "hoge", null},
                @"[1,""hoge"",null]",
            },
            new object[] {
                new int[] {1, 2, 3},
                @"[1,2,3]",
            },
            new object[] {
                new List<int> {1, 2, 3},
                @"[1,2,3]",
            },
            new object[] {
                new List<Dictionary<string, int>> {
                    new Dictionary<string, int> {
                        {"a", 1},
                    },
                    new Dictionary<string, int> {
                        {"b", 2},
                    },
                    new Dictionary<string, int> {
                        {"c", 3},
                    },
                },
                @"[{""a"":1},{""b"":2},{""c"":3}]",
            },
        };

        public static object[] DictArgs = {
            new object[] {
                new Dictionary<string, object>(),
                @"{}",
            },
            new object[] {
                new Dictionary<string, object> {
                    {"X", 10},
                    {"Y", "abab"},
                },
                @"{""X"":10,""Y"":""abab""}",
            },
        };
    }

    class JsonSerializerErrorCaseTests
    {
        [Test]
        [TestCaseSource("CommonArgs")]
        public void DeserializeFailureTest(Type ty, string json, string errorMsg)
        {
            var s = new JsonSerializer(ty);
            var ex = Assert.Throws<DeserializeFailureException>(() => s.Deserialize(json));

            Assert.AreEqual(errorMsg, ex.Message);
        }

        //
        static object[] CommonArgs = {
            new object[] {
                typeof(bool),
                "1",
                "(root): Integer cannot convert to System.Boolean.",
            },
            new object[] {
                typeof(bool),
                "null",
                "(root): Null cannot convert to non-nullable value(System.Boolean).",
            },
            new object[] {
                typeof(int),
                "true",
                "(root): Boolean cannot convert to System.Int32.",
            },
            new object[] {
                typeof(int),
                "3.14",
                "(root): System.Double cannot convert to System.Int32.",
            },
            new object[] {
                typeof(int),
                "null",
                "(root): Null cannot convert to non-nullable value(System.Int32).",
            },
            new object[] {
                typeof(string),
                "true",
                "(root): Boolean cannot convert to System.String.",
            },
            new object[] {
                typeof(string[]),
                "true",
                "(root): Boolean cannot convert to System.String[].",
            },
            new object[] {
                typeof(List<string>),
                "true",
                "(root): Boolean cannot convert to System.Collections.Generic.List`1[System.String].",
            },
            new object[] {
                typeof(string[]),
                "[\"\", 1]",
                "(root)[1]: Integer cannot convert to System.String.",
            },
            new object[] {
                typeof(List<string>),
                "[\"\", 1]",
                "(root)[1]: Integer cannot convert to System.String.",
            },
            new object[] {
                typeof(Dictionary<string, int>),
                "true",
                "(root): System.Boolean cannot convert to System.Collections.Generic.Dictionary`2[System.String,System.Int32].",
            },
            new object[] {
                typeof(Dictionary<string, int>),
                "{\"a\": 1, \"b\": \"bo\"}",
                "(root)[\"b\"]: String cannot convert to System.Int32.",
            },
            new object[] {
                typeof(SomeObject),
                "true",
                "(root): System.Boolean cannot convert to VJson.UnitTests.SomeObject.",
            },
            new object[] {
                typeof(SomeObject),
                "{\"X\": \"bo\"}",
                "(root)[\"X\"]: String cannot convert to System.Int32.",
            },
            new object[] {
                typeof(CustomObject),
                "{\"Obj\": \"bo\"}",
                "(root)[\"Obj\"]: String cannot convert to one of [System.Boolean, VJson.UnitTests.SomeObject].",
            },
            new object[] {
                typeof(Hoge),
                "42",
                "(root): System.Int64 cannot convert implicitly to VJson.UnitTests.Hoge.",
            },
        };
    }
}
