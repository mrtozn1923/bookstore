using System.Text.Json.Nodes;

namespace Bookstore.Api.Common;

// RFC 7396 section 2: null removes a member, objects merge recursively, everything else replaces.
public static class MergePatchDocument
{
    public static JsonNode? Apply(JsonNode? target, JsonNode? patch)
    {
        if (patch is not JsonObject patchObject)
        {
            return patch?.DeepClone();
        }

        JsonObject result = target as JsonObject is { } targetObject
            ? (JsonObject)targetObject.DeepClone()
            : [];
        foreach ((string name, JsonNode? value) in patchObject)
        {
            if (value is null)
            {
                result.Remove(name);
            }
            else
            {
                result[name] = Apply(result[name], value);
            }
        }

        return result;
    }
}
