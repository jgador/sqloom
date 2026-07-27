using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;

namespace Sqloom.Host.QueryStore;

internal static class SqlServerQueryTypeMaps
{
    static SqlServerQueryTypeMaps()
    {
        Register<QueryStoreDatabaseOptionsRow>(
            ("desired_state_desc", row => row.DesiredState),
            ("actual_state_desc", row => row.ActualState),
            ("readonly_reason", row => row.ReadOnlyReason),
            ("current_storage_size_mb", row => row.CurrentStorageSizeMb),
            ("max_storage_size_mb", row => row.MaxStorageSizeMb));

        Register<QueryStorePlanRow>(
            ("query_id", row => row.QueryId),
            ("plan_id", row => row.PlanId),
            ("query_text_id", row => row.QueryTextId),
            ("statement_sql_handle", row => row.StatementSqlHandle),
            ("object_id", row => row.ObjectId),
            ("query_parameterization_type", row => row.QueryParameterizationType),
            ("query_parameterization_type_desc", row => row.ParamTypeDescription),
            ("query_hash", row => row.QueryHash),
            ("query_sql_text", row => row.QueryText),
            ("object_name", row => row.ObjectName),
            ("execution_count", row => row.ExecutionCount),
            ("mean_duration_us", row => row.MeanDurationMicroseconds),
            ("max_duration_us", row => row.MaxDurationMicroseconds),
            ("mean_cpu_us", row => row.MeanCpuMicroseconds),
            ("mean_logical_reads", row => row.MeanLogicalReads),
            ("last_execution_time", row => row.LastExecutionTimeUtc));

        Register<QueryStoreWaitRow>(
            ("query_id", row => row.QueryId),
            ("plan_id", row => row.PlanId),
            ("wait_category_desc", row => row.WaitCategory),
            ("avg_query_wait_time_ms", row => row.AvgWaitMs),
            ("total_query_wait_time_ms", row => row.TotalWaitMilliseconds));

        Register<DiscoveredDatabaseObjectRow>(
            ("schema_name", row => row.SchemaName),
            ("object_name", row => row.ObjectName),
            ("object_kind", row => row.ObjectKind));

        Register<ViewDefinitionPermissionRow>(
            ("has_view_definition", row => row.HasViewDefinition));

        Register<SqlStatementHandleRow>(
            ("query_parameterization_type", row => row.QueryParameterizationType),
            ("statement_sql_handle", row => row.StatementSqlHandle));
    }

    internal static void EnsureRegistered()
    {
    }

    private static void Register<T>(
        params (string ColumnName, Expression<Func<T, object?>> Selector)[] mappings)
    {
        Dictionary<string, PropertyInfo> properties = mappings.ToDictionary(
            mapping => mapping.ColumnName,
            mapping => Property(mapping.Selector),
            StringComparer.OrdinalIgnoreCase);

        SqlMapper.SetTypeMap(typeof(T), new ExplicitColumnTypeMap<T>(properties));
    }

    private static PropertyInfo Property<T>(Expression<Func<T, object?>> selector)
    {
        Expression body = selector.Body is UnaryExpression conversion
            ? conversion.Operand
            : selector.Body;

        return body is MemberExpression { Member: PropertyInfo property }
            ? property
            : throw new ArgumentException("Selector must reference a property.", nameof(selector));
    }

    private sealed class ExplicitColumnTypeMap<T> : SqlMapper.ITypeMap
    {
        private readonly DefaultTypeMap _constructorMap = new(typeof(T));
        private readonly CustomPropertyTypeMap _propertyMap;

        public ExplicitColumnTypeMap(IReadOnlyDictionary<string, PropertyInfo> properties)
        {
            _propertyMap = new CustomPropertyTypeMap(
                typeof(T),
                (_, columnName) => properties.GetValueOrDefault(columnName)!);
        }

        public ConstructorInfo? FindConstructor(string[] names, Type[] types)
        {
            return _constructorMap.FindConstructor(names, types);
        }

        public ConstructorInfo? FindExplicitConstructor()
        {
            return _constructorMap.FindExplicitConstructor();
        }

        public SqlMapper.IMemberMap GetConstructorParameter(
            ConstructorInfo constructor,
            string columnName)
        {
            return _constructorMap.GetConstructorParameter(constructor, columnName);
        }

        public SqlMapper.IMemberMap GetMember(string columnName)
        {
            return _propertyMap.GetMember(columnName)
                ?? throw new InvalidOperationException(
                    $"Column '{columnName}' has no mapping for {typeof(T).Name}.");
        }
    }
}
