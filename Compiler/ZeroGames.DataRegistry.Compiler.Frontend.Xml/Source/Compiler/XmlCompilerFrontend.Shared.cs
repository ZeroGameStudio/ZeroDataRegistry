// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

public partial class XmlCompilerFrontend
{
	
	private string GetName(XElement element)
	{
		string? name = element.Attribute(NAME_ATTRIBUTE_NAME)?.Value;
		if (!IsValidIdentifier(name))
		{
			throw new ParserException();
		}

		return name;
	}
	
	private string GetNamespace(XElement element)
	{
		string? name = element.Attribute(NAMESPACE_ATTRIBUTE_NAME)?.Value;
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}
		
		if (!IsValidNamespace(name))
		{
			throw new ParserException();
		}

		return name;
	}
	
	private bool IsValidIdentifier([NotNullWhen(true)] string? identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
		{
			return false;
		}

		if (_reservedKeywords.Contains(identifier))
		{
			return false;
		}

		return _identifierRegex.IsMatch(identifier);
	}
	
	private bool IsValidNamespace([NotNullWhen(true)] string? identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
		{
			return false;
		}

		if (_reservedKeywords.Contains(identifier))
		{
			return false;
		}

		return _namespaceRegex.IsMatch(identifier);
	}

	private const string SCHEMA_ELEMENT_NAME = "Schema";
	private const string IMPORT_ELEMENT_NAME = "Import";
	private const string METADATA_ELEMENT_NAME = "Metadata";
	private const string ENTITY_ELEMENT_NAME = "Entity";
	private const string STRUCT_ELEMENT_NAME = "Struct";
	private const string ENUM_ELEMENT_NAME = "Enum";
	private const string PRIMARY_KEY_ELEMENT_NAME = "PrimaryKey";
	private const string FOREIGN_KEY_ELEMENT_NAME = "ForeignKey";
	private const string PROPERTY_ELEMENT_NAME = "Property";
	private const string ENUM_ELEMENT_ELEMENT_NAME = "Element";

	private const string URI_ATTRIBUTE_NAME = "Uri";
	private const string ALIAS_ATTRIBUTE_NAME = "Alias";
	private const string NAME_ATTRIBUTE_NAME = "Name";
	private const string NAMESPACE_ATTRIBUTE_NAME = "Namespace";
	private const string TYPE_ATTRIBUTE_NAME = "Type";
	private const string EXTENDS_ATTRIBUTE_NAME = "Extends";
	private const string ABSTRACT_ATTRIBUTE_NAME = "Abstract";
	private const string KEY_ATTRIBUTE_NAME = "Key";
	private const string VALUE_ATTRIBUTE_NAME = "Value";

	[GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
	private static partial Regex _identifierRegex { get; }
	
	[GeneratedRegex("^([A-Za-z_][A-Za-z0-9_]*)(\\.[A-Za-z_][A-Za-z0-9_]*)*$")]
	private static partial Regex _namespaceRegex { get; }
	
	private static readonly string[] _reservedKeywords = [
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
		"decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", 
		"fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
		"long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", 
		"public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
		"switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
		"void", "volatile", "while"
	];
	
}


