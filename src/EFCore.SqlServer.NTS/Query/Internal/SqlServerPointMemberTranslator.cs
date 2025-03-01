// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NetTopologySuite.Geometries;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

internal class SqlServerPointMemberTranslator : IMemberTranslator
{
    private static readonly IDictionary<MemberInfo, string> MemberToPropertyName = new Dictionary<MemberInfo, string>
    {
        { typeof(Point).GetRequiredRuntimeProperty(nameof(Point.M)), "M" },
        { typeof(Point).GetRequiredRuntimeProperty(nameof(Point.Z)), "Z" }
    };

    private static readonly IDictionary<MemberInfo, string> GeographyMemberToPropertyName = new Dictionary<MemberInfo, string>
    {
        { typeof(Point).GetRequiredRuntimeProperty(nameof(Point.X)), "Long" },
        { typeof(Point).GetRequiredRuntimeProperty(nameof(Point.Y)), "Lat" }
    };

    private static readonly IDictionary<MemberInfo, string> GeometryMemberToPropertyName = new Dictionary<MemberInfo, string>
    {
        { typeof(Point).GetRequiredRuntimeProperty(nameof(Point.X)), "STX" },
        { typeof(Point).GetRequiredRuntimeProperty(nameof(Point.Y)), "STY" }
    };

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    public SqlServerPointMemberTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (typeof(Point).IsAssignableFrom(member.DeclaringType))
        {
            Check.DebugAssert(instance!.TypeMapping != null, "Instance must have typeMapping assigned.");
            var storeType = instance.TypeMapping.StoreType;
            var isGeography = string.Equals(storeType, "geography", StringComparison.OrdinalIgnoreCase);

            if (MemberToPropertyName.TryGetValue(member, out var propertyName)
                || (isGeography
                    ? GeographyMemberToPropertyName.TryGetValue(member, out propertyName)
                    : GeometryMemberToPropertyName.TryGetValue(member, out propertyName))
                && propertyName != null)
            {
                return _sqlExpressionFactory.NiladicFunction(
                    instance,
                    propertyName,
                    nullable: true,
                    instancePropagatesNullability: true,
                    returnType);
            }
        }

        return null;
    }
}
