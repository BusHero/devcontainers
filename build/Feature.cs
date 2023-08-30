using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

/// <summary>
/// Development Container Features Metadata (devcontainer-feature.json). See
/// https://containers.dev/implementors/features/ for more information.
/// </summary>
public partial record Feature
{
    /// <summary>
    /// Passes docker capabilities to include when creating the dev container.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("capAdd")]
    public string[]? CapAdd { get; init; }

    /// <summary>
    /// Container environment variables.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("containerEnv")]
    public Dictionary<string, string>? ContainerEnv { get; set; } = null!;

    /// <summary>
    /// Tool-specific configuration. Each tool should use a JSON object subproperty with a unique
    /// name to group its customizations.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("customizations")]
    public Dictionary<string, object>? Customizations { get; set; } = null!;

    /// <summary>
    /// Indicates that the Feature is deprecated, and will not receive any further
    /// updates/support. This property is intended to be used by the supporting tools for
    /// highlighting Feature deprecation.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("deprecated")]
    public bool? Deprecated { get; set; }

    /// <summary>
    /// Description of the Feature. For the best appearance in an implementing tool, refrain from
    /// including markdown or HTML in the description.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// URL to documentation for the Feature.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("documentationURL")]
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Entrypoint script that should fire at container start up.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("entrypoint")]
    public string? Entrypoint { get; set; }

    /// <summary>
    /// ID of the Feature. The id should be unique in the context of the repository/published
    /// package where the feature exists and must match the name of the directory where the
    /// devcontainer-feature.json resides.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Adds the tiny init process to the container (--init) when the Feature is used.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("init")]
    public bool? Init { get; set; }

    /// <summary>
    /// Array of ID's of Features that should execute before this one. Allows control for feature
    /// authors on soft dependencies between different Features.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("installsAfter")]
    public string[]? InstallsAfter { get; set; }

    /// <summary>
    /// Array of old IDs used to publish this Feature. The property is useful for renaming a
    /// currently published Feature within a single namespace.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("legacyIds")]
    public string[]? LegacyIds { get; set; }

    /// <summary>
    /// URL to the license for the Feature.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("licenseURL")]
    public string? LicenseUrl { get; set; }

    /// <summary>
    /// Mounts a volume or bind mount into the container.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("mounts")]
    public Mount[]? Mounts { get; set; }

    /// <summary>
    /// Display name of the Feature.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// A command to run when creating the container. This command is run after
    /// "initializeCommand" and before "updateContentCommand". If this is a single string, it
    /// will be run in a shell. If this is an array of strings, it will be run as a single
    /// command without shell. If this is an object, each provided command will be run in
    /// parallel.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("onCreateCommand")]
    public CoordinateOnCreateCommand? OnCreateCommand { get; set; }

    /// <summary>
    /// Possible user-configurable options for this Feature. The selected options will be passed
    /// as environment variables when installing the Feature into the container.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("options")]
    public Dictionary<string, FeatureOption>? Options { get; set; }

    /// <summary>
    /// A command to run when attaching to the container. This command is run after
    /// "postStartCommand". If this is a single string, it will be run in a shell. If this is an
    /// array of strings, it will be run as a single command without shell. If this is an object,
    /// each provided command will be run in parallel.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("postAttachCommand")]
    public CoordinateOnCreateCommand? PostAttachCommand { get; set; }

    /// <summary>
    /// A command to run after creating the container. This command is run after
    /// "updateContentCommand" and before "postStartCommand". If this is a single string, it will
    /// be run in a shell. If this is an array of strings, it will be run as a single command
    /// without shell. If this is an object, each provided command will be run in parallel.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("postCreateCommand")]
    public CoordinateOnCreateCommand? PostCreateCommand { get; set; }

    /// <summary>
    /// A command to run after starting the container. This command is run after
    /// "postCreateCommand" and before "postAttachCommand". If this is a single string, it will
    /// be run in a shell. If this is an array of strings, it will be run as a single command
    /// without shell. If this is an object, each provided command will be run in parallel.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("postStartCommand")]
    public CoordinateOnCreateCommand? PostStartCommand { get; set; }

    /// <summary>
    /// Sets privileged mode (--privileged) for the container.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("privileged")]
    public bool? Privileged { get; set; }

    /// <summary>
    /// Sets container security options to include when creating the container.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("securityOpt")]
    public string[]? SecurityOpt { get; set; }

    /// <summary>
    /// A command to run when creating the container and rerun when the workspace content was
    /// updated while creating the container. This command is run after "onCreateCommand" and
    /// before "postCreateCommand". If this is a single string, it will be run in a shell. If
    /// this is an array of strings, it will be run as a single command without shell. If this is
    /// an object, each provided command will be run in parallel.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("updateContentCommand")]
    public CoordinateOnCreateCommand? UpdateContentCommand { get; set; }

    /// <summary>
    /// The version of the Feature. Follows the semanatic versioning (semver) specification.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

/// <summary>
/// Mounts a volume or bind mount into the container.
/// </summary>
public partial class Mount
{
    /// <summary>
    /// Mount source.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; set; }

    /// <summary>
    /// Mount target.
    /// </summary>
    [JsonPropertyName("target")]
    public required string Target { get; set; }

    /// <summary>
    /// Type of mount. Can be 'bind' or 'volume'.
    /// </summary>
    [JsonPropertyName("type")]
    public MountType Type { get; set; }
}

/// <summary>
/// Option value is represented with a boolean value.
/// </summary>
public partial class FeatureOption
{
    /// <summary>
    /// Default value if the user omits this option from their configuration.
    /// </summary>
    [JsonPropertyName("default")]
    public Default Default { get; set; }

    /// <summary>
    /// A description of the option displayed to the user by a supporting tool.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The type of the option. Can be 'boolean' or 'string'.  Options of type 'string' should
    /// use the 'enum' or 'proposals' property to provide a list of allowed values.
    /// </summary>
    [JsonPropertyName("type")]
    public FeatureOptionType Type { get; set; }

    /// <summary>
    /// Allowed values for this option.  Unlike 'proposals', the user cannot provide a custom
    /// value not included in the 'enum' array.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("enum")]
    public string[]? Enum { get; set; }

    /// <summary>
    /// Suggested values for this option.  Unlike 'enum', the 'proposals' attribute indicates the
    /// installation script can handle arbitrary values provided by the user.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("proposals")]
    public string[]? Proposals { get; set; }
}

/// <summary>
/// Type of mount. Can be 'bind' or 'volume'.
/// </summary>
[JsonConverter(typeof(MountTypeConverter))]
public enum MountType { Bind, Volume };

[JsonConverter(typeof(FeatureOptionTypeConverter))]
public enum FeatureOptionType { Boolean, String };

[JsonConverter(typeof(OnCreateCommandValueConverter))]
public partial struct OnCreateCommandValue
{
    public string? String;
    public string[]? StringArray;

    public static implicit operator OnCreateCommandValue(string String) => new OnCreateCommandValue { String = String };
    public static implicit operator OnCreateCommandValue(string[] StringArray) => new OnCreateCommandValue { StringArray = StringArray };
}

/// <summary>
/// A command to run when creating the container. This command is run after
/// "initializeCommand" and before "updateContentCommand". If this is a single string, it
/// will be run in a shell. If this is an array of strings, it will be run as a single
/// command without shell. If this is an object, each provided command will be run in
/// parallel.
///
/// A command to run when attaching to the container. This command is run after
/// "postStartCommand". If this is a single string, it will be run in a shell. If this is an
/// array of strings, it will be run as a single command without shell. If this is an object,
/// each provided command will be run in parallel.
///
/// A command to run after creating the container. This command is run after
/// "updateContentCommand" and before "postStartCommand". If this is a single string, it will
/// be run in a shell. If this is an array of strings, it will be run as a single command
/// without shell. If this is an object, each provided command will be run in parallel.
///
/// A command to run after starting the container. This command is run after
/// "postCreateCommand" and before "postAttachCommand". If this is a single string, it will
/// be run in a shell. If this is an array of strings, it will be run as a single command
/// without shell. If this is an object, each provided command will be run in parallel.
///
/// A command to run when creating the container and rerun when the workspace content was
/// updated while creating the container. This command is run after "onCreateCommand" and
/// before "postCreateCommand". If this is a single string, it will be run in a shell. If
/// this is an array of strings, it will be run as a single command without shell. If this is
/// an object, each provided command will be run in parallel.
/// </summary>
[JsonConverter(typeof(CoordinateOnCreateCommandConverter))]
public partial struct CoordinateOnCreateCommand
{
    public Dictionary<string, OnCreateCommandValue>? AnythingMap;
    public string? String;
    public string[]? StringArray;

    public static implicit operator CoordinateOnCreateCommand(Dictionary<string, OnCreateCommandValue> AnythingMap) => new() { AnythingMap = AnythingMap };
    public static implicit operator CoordinateOnCreateCommand(string String) => new() { String = String };
    public static implicit operator CoordinateOnCreateCommand(string[] StringArray) => new() { StringArray = StringArray };
}

[JsonConverter(typeof(DefaultConverter))]
public partial struct Default
{
    public bool? Bool;
    public string? String;

    public static implicit operator Default(bool Bool) => new() { Bool = Bool };
    public static implicit operator Default(string String) => new() { String = String };

    public readonly string GetValue() => Bool?.ToString() ?? String ?? "-";
}

internal static class Converter
{
    public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
    {
        Converters =
            {
                new DateOnlyConverter(),
                new TimeOnlyConverter(),
            },
    };
}

internal class MountTypeConverter : JsonConverter<MountType>
{
    public override bool CanConvert(Type t) => t == typeof(MountType);

    public override MountType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "bind" => MountType.Bind,
            "volume" => MountType.Volume,
            _ => throw new Exception("Cannot unmarshal type MountType"),
        };
    }

    public override void Write(Utf8JsonWriter writer, MountType value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case MountType.Bind:
                JsonSerializer.Serialize(writer, "bind", options);
                return;
            case MountType.Volume:
                JsonSerializer.Serialize(writer, "volume", options);
                return;
        }
        throw new Exception("Cannot marshal type MountType");
    }

    public static readonly MountTypeConverter Singleton = new();
}

internal class CoordinateOnCreateCommandConverter : JsonConverter<CoordinateOnCreateCommand>
{
    public override bool CanConvert(Type t) => t == typeof(CoordinateOnCreateCommand);

    public override CoordinateOnCreateCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new CoordinateOnCreateCommand
            {
                String = reader.GetString()
            },
            JsonTokenType.StartObject => new CoordinateOnCreateCommand
            {
                AnythingMap = JsonSerializer.Deserialize<Dictionary<string, OnCreateCommandValue>>(ref reader, options)
            },
            JsonTokenType.StartArray => new CoordinateOnCreateCommand
            {
                StringArray = JsonSerializer.Deserialize<string[]>(ref reader, options)
            },
            _ => throw new Exception("Cannot unmarshal type CoordinateOnCreateCommand"),
        };
    }

    public override void Write(Utf8JsonWriter writer, CoordinateOnCreateCommand value, JsonSerializerOptions options)
    {
        if (value.String != null)
        {
            JsonSerializer.Serialize(writer, value.String, options);
            return;
        }
        if (value.StringArray != null)
        {
            JsonSerializer.Serialize(writer, value.StringArray, options);
            return;
        }
        if (value.AnythingMap != null)
        {
            JsonSerializer.Serialize(writer, value.AnythingMap, options);
            return;
        }
        throw new Exception("Cannot marshal type CoordinateOnCreateCommand");
    }

    public static readonly CoordinateOnCreateCommandConverter Singleton = new();
}

internal class OnCreateCommandValueConverter : JsonConverter<OnCreateCommandValue>
{
    public override bool CanConvert(Type t) => t == typeof(OnCreateCommandValue);

    public override OnCreateCommandValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new OnCreateCommandValue
            {
                String = reader.GetString()
            },
            JsonTokenType.StartArray => new OnCreateCommandValue
            {
                StringArray = JsonSerializer.Deserialize<string[]>(ref reader, options)
            },
            _ => throw new Exception("Cannot unmarshal type OnCreateCommandValue"),
        };
    }

    public override void Write(Utf8JsonWriter writer, OnCreateCommandValue value, JsonSerializerOptions options)
    {
        if (value.String != null)
        {
            JsonSerializer.Serialize(writer, value.String, options);
            return;
        }
        if (value.StringArray != null)
        {
            JsonSerializer.Serialize(writer, value.StringArray, options);
            return;
        }
        throw new Exception("Cannot marshal type OnCreateCommandValue");
    }
}

internal class DefaultConverter : JsonConverter<Default>
{
    public override bool CanConvert(Type t) => t == typeof(Default);

    public override Default Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => new Default
            {
                Bool = reader.GetBoolean()
            },
            JsonTokenType.String => new Default
            {
                String = reader.GetString()
            },
            _ => throw new Exception("Cannot unmarshal type Default"),
        };
    }

    public override void Write(Utf8JsonWriter writer, Default value, JsonSerializerOptions options)
    {
        if (value.Bool != null)
        {
            JsonSerializer.Serialize(writer, value.Bool.Value, options);
            return;
        }
        if (value.String != null)
        {
            JsonSerializer.Serialize(writer, value.String, options);
            return;
        }
        throw new Exception("Cannot marshal type Default");
    }

    public static readonly DefaultConverter Singleton = new();
}

internal class FeatureOptionTypeConverter : JsonConverter<FeatureOptionType>
{
    public override bool CanConvert(Type t) => t == typeof(FeatureOptionType);

    public override FeatureOptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "boolean" => FeatureOptionType.Boolean,
            "string" => FeatureOptionType.String,
            _ => throw new Exception("Cannot unmarshal type FeatureOptionType"),
        };
    }

    public override void Write(Utf8JsonWriter writer, FeatureOptionType value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case FeatureOptionType.Boolean:
                JsonSerializer.Serialize(writer, "boolean", options);
                return;
            case FeatureOptionType.String:
                JsonSerializer.Serialize(writer, "string", options);
                return;
        }
        throw new Exception("Cannot marshal type FeatureOptionType");
    }

    public static readonly FeatureOptionTypeConverter Singleton = new();
}

public class DateOnlyConverter : JsonConverter<DateOnly>
{
    private readonly string serializationFormat;
    public DateOnlyConverter() : this(null) { }

    public DateOnlyConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}

public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private readonly string serializationFormat;

    public TimeOnlyConverter() : this(null) { }

    public TimeOnlyConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
    }

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return TimeOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}

internal class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

    private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

    private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
    private string? _dateTimeFormat;
    private CultureInfo? _culture;

    public DateTimeStyles DateTimeStyles
    {
        get => _dateTimeStyles;
        set => _dateTimeStyles = value;
    }

    public string? DateTimeFormat
    {
        get => _dateTimeFormat ?? string.Empty;
        set => _dateTimeFormat = string.IsNullOrEmpty(value) ? null : value;
    }

    public CultureInfo Culture
    {
        get => _culture ?? CultureInfo.CurrentCulture;
        set => _culture = value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        string text;


        if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
            || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
        {
            value = value.ToUniversalTime();
        }

        text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

        writer.WriteStringValue(text);
    }

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateText = reader.GetString();

        if (string.IsNullOrEmpty(dateText) == false)
        {
            if (!string.IsNullOrEmpty(_dateTimeFormat))
            {
                return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
            }
            else
            {
                return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
            }
        }
        else
        {
            return default;
        }
    }


    public static readonly IsoDateTimeOffsetConverter Singleton = new();
}
