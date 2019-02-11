//
// Copyright (c) 2019- yutopp (yutopp@gmail.com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at  https://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace VJson.Schema
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Field)]
    public class Schema : JsonSchema
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ItemsSchema : Schema
    {
    }
}
