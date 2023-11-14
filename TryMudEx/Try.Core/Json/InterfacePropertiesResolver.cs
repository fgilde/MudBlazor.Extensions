using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Try.Core.Json;

public class InterfacePropertiesResolver : DefaultContractResolver
{
    private Type _interfaceType;

    public InterfacePropertiesResolver(Type interfaceType)
    {
        _interfaceType = interfaceType;
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(_interfaceType, memberSerialization);
        return properties;
    }
}