using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Dapper.Contrib.Extensions;
using FactoryGirlExt.Helpers;

namespace FactoryGirlExt
{
    public static class FactoryGirl
    {
        private static readonly IDictionary<Type, Func<object>> Builders = new Dictionary<Type, Func<object>>();

        private static readonly IList CreationBuilders = new List<object>();

        public static IEnumerable<Type> DefinedFactories
        {
            get { return Builders.Select(x => x.Key); }
        }

        public static void Define<T>(Func<T> builder)
        {
            if (Builders.ContainsKey(typeof(T)))
                throw new DuplicateFactoryException(typeof(T).Name +
                                                    " is already registered. You can only register one factory per type.");
            Builders.Add(typeof(T), () => builder());
        }

        public static T Build<T>()
        {
            return Build<T>(x => { });
        }

        public static T Build<T>(Action<T> overrides)
        {
            var result = (T)Builders[typeof(T)]();
            overrides(result);
            return result;
        }

        private static PropertyContainer ParseProperties<T>(T obj)
        {
            var propertyContainer = new PropertyContainer();

            var typeName = typeof(T).Name;
            var validKeyNames = new[]
            {
                "Id",
                string.Format("{0}Id", typeName), string.Format("{0}_Id", typeName)
            };

            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                // Skip reference types (but still include string!)
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    continue;

                // Skip methods without a public setter
                if (property.GetSetMethod() == null)
                    continue;

                if (property.IsDefined(typeof(ComputedAttribute), false))
                    continue;

                //// Skip methods specifically ignored
                //if (property.IsDefined(typeof (DapperIgnore), false))
                //    continue;

                var name = property.Name;
                var value = typeof(T).GetProperty(property.Name).GetValue(obj, null);

                if (property.PropertyType.IsEnum)
                {
                    value = value.ToString();
                }

                if (validKeyNames.Contains(name) || property.IsDefined(typeof(KeyAttribute), false))
                {
                    propertyContainer.AddId(name, value);
                }
                else
                {
                    propertyContainer.AddValue(name, value);
                }
            }

            return propertyContainer;
        }

        private static PropertyContainer ParseProperties(object obj)
        {
            var propertyContainer = new PropertyContainer();

            var typeName = obj.GetType().Name;
            var validKeyNames = new[]
            {
                "Id",
                string.Format("{0}Id", typeName), string.Format("{0}_Id", typeName)
            };

            var properties = obj.GetType().GetProperties();
            foreach (var property in properties)
            {
                // Skip reference types (but still include string!)
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    continue;

                // Skip methods without a public setter
                if (property.GetSetMethod() == null)
                    continue;

                //// Skip methods specifically ignored
                //if (property.IsDefined(typeof (DapperIgnore), false))
                //    continue;

                var name = property.Name;
                var value = obj.GetType().GetProperty(property.Name).GetValue(obj, null);

                if (property.PropertyType.IsEnum)
                {
                    value = value.ToString();
                }

                if (validKeyNames.Contains(name) || property.IsDefined(typeof(KeyAttribute), false))
                {
                    propertyContainer.AddId(name, value);
                }
                else
                {
                    propertyContainer.AddValue(name, value);
                }
            }

            return propertyContainer;
        }

        private static void SetId<T>(T obj, dynamic id, IDictionary<string, object> propertyPairs)
        {
            if (propertyPairs.Count == 1)
            {
                var propertyName = propertyPairs.Keys.First();
                var propertyInfo = obj.GetType().GetProperty(propertyName);
                propertyInfo.SetValue(obj, id, null);
            }
        }

        private static string GetTableName<T>()
        {
            return GetTableNameFor(typeof(T));
        }

        private static string GetTableNameFor(Type type)
        {
            var dnAttribute = type.GetCustomAttributes(
                typeof(TableAttribute), true
                ).FirstOrDefault() as TableAttribute;

            if (dnAttribute != null)
            {
                return dnAttribute.Name;
            }

            return type.Name;
        }

        /// <summary>
        /// Create an entiy to DB with an identity, return it back to application
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repo"></param>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        /// <returns>Entity</returns>
        public static T CreateGeneric<T>(string connectionString, T obj)
        {
            var propertyContainer = ParseProperties(obj);

            var sql = string.Format("INSERT INTO [{0}] ([{1}]) VALUES (@{2}) SELECT CAST(scope_identity() AS int)",
                GetTableName<T>(),
                string.Join("],[", propertyContainer.ValueNames),
                string.Join(", @", propertyContainer.ValueNames));

            dynamic rest;

            using (var connection = new SqlConnection(connectionString))
            {
                rest = connection.Query(sql, propertyContainer.ValuePairs).First();
            }

            var row = rest as IDictionary<string, object>;

            if (rest == null || row == null)
                throw new Exception("Cannot insert to DB");

            SetId(obj, row.First().Value, propertyContainer.IdPairs);

            CreationBuilders.Add(obj);

            return obj;
        }

        /// <summary>
        /// Create a data to DB with an identity, return it back to application
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="overrides"></param>
        /// <returns>Entity</returns>
        public static T CreateGeneric<T>(string connectionString, Action<T> overrides)
        {
            return CreateGeneric(connectionString, Build(overrides));
        }

        /// <summary>
        /// Create an entity to DB without auto-identity, return it back to app
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        /// <returns>Entity</returns>
        public static T CreateGenericWithoutAutoIndetity<T>(string connectionString, T obj)
        {
            var propertyContainer = ParseProperties(obj);
            var sql = string.Format("INSERT INTO [{0}] ([{1}]) OUTPUT INSERTED.{3} VALUES (@{2}) ",
                GetTableName<T>(),
                string.Join("],[", propertyContainer.AllNames),
                string.Join(", @", propertyContainer.AllNames),
                string.Join("", propertyContainer.IdNames));

            dynamic rest;

            using (var connection = new SqlConnection(connectionString))
            {
                rest = connection.Query(sql, propertyContainer.AllPairs).First();
            }
            var row = rest as IDictionary<string, object>;

            if (rest == null || row == null)
                throw new Exception("Cannot insert to DB");

            SetId(obj, row.First().Value, propertyContainer.IdPairs);

            CreationBuilders.Add(obj);

            return obj;
        }

        /// <summary>
        /// Create an entity to DB without auto-identity, return it back to app
        /// It is shortcut way to create an entity in memory and db, without call Build function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="overrides"></param>
        /// <returns>Entity</returns>
        public static T CreateGenericWithoutAutoIndetity<T>(string connectionString, Action<T> overrides)
        {
            return CreateGenericWithoutAutoIndetity(connectionString, Build(overrides));
        }

        /// <summary>
        /// Insert data to DB by using script, with auto identity creation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="obj"></param>
        /// <returns>Entity</returns>
        public static T Create<T>(string connectionString, string sql, object param, T obj)
        {
            var propertyContainer = ParseProperties(obj);

            dynamic rest;

            using (var connection = new SqlConnection(connectionString))
            {
                rest = connection.Query(sql, param).First();
            }

            var row = rest as IDictionary<string, object>;

            if (rest == null || row == null)
                throw new Exception("Cannot insert to DB");

            SetId(obj, row.First().Value, propertyContainer.IdPairs);

            CreationBuilders.Add(obj);

            return obj;
        }

        /// <summary>
        /// Delete and create an entity without auto-identity, return it back to app
        /// If exist any data in db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        /// <returns>Entity</returns>
        public static T DeleteThenCreateWithoutAutoIdentity<T>(string connectionString, T obj)
        {
            Delete(connectionString, obj);
            return CreateGenericWithoutAutoIndetity(connectionString, obj);
        }

        /// <summary>
        /// Delete and create an overrited entity with same identity, return it back to app
        /// If exist any data in db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        /// <returns>Entity</returns>
        public static T DeleteThenCreateWithExplicitIdentity<T>(string connectionString, T obj)
        {
            Delete(connectionString, obj);

            var propertyContainer = ParseProperties(obj);
            var sql = string.Format("SET IDENTITY_INSERT [{0}] ON;" +
                                    "INSERT INTO [{0}] ([{1}]) OUTPUT INSERTED.{3} VALUES (@{2});" +
                                    "SET IDENTITY_INSERT [{0}] OFF; ",
                GetTableName<T>(),
                string.Join("],[", propertyContainer.AllNames),
                string.Join(", @", propertyContainer.AllNames),
                string.Join("", propertyContainer.IdNames));

            dynamic rest;

            using (var connection = new SqlConnection(connectionString))
            {
                rest = connection.Query(sql, propertyContainer.AllPairs).First();
            }

            var row = rest as IDictionary<string, object>;

            if (rest == null || row == null)
                throw new Exception("Cannot insert to DB");

            SetId(obj, row.First().Value, propertyContainer.IdPairs);

            CreationBuilders.Add(obj);

            return obj;
        }

        /// <summary>
        /// Query an entity based on own identity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        /// <param name="pair"></param>
        /// <returns>Entity</returns>
        public static T Select<T>(string connectionString, T obj, IDictionary<string, object> pair) where T : class, new()
        {
            var sql = string.Format("SELECT * FROM [{0}] WHERE {1} = @{2}",
                typeof(T).Name,
                pair.Keys.First(),
                pair.Keys.First());

            dynamic rest;

            using (var connection = new SqlConnection(connectionString))
            {
                rest = connection.Query(sql, pair).First();
            }

            if (rest == null)
                throw new Exception("Cannot find object");

            return DapperHelpers.CastDynamicToClass<T>(rest);
        }

        /// <summary>
        /// Query an entity based on own properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        /// <returns>Entity</returns>
        public static T Get<T>(string connectionString, T obj) where T : class, new()
        {
            var propertyContainer = ParseProperties(obj);

            var whereClause =
                string.Join(" AND ", propertyContainer.IdPairs.Select(
                    pair =>
                        string.Format("{0} = {1}", pair.Key,
                            pair.Value.ToSqlQueryCompatible())));

            var sql = string.Format(@"SET ANSI_NULLS OFF; SELECT * FROM [{0}] WHERE {1}; SET ANSI_NULLS ON;",
                GetTableName<T>(), whereClause);

            dynamic result;

            using (var connection = new SqlConnection(connectionString))
            {
                result = connection.Query(sql).FirstOrDefault();
            }

            if (result == null)
                throw new Exception("Cannot find object");

            return DapperHelpers.CastDynamicToClass<T>(result);
        }

        /// <summary>
        /// Delete an entity based on own identity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        public static void Delete<T>(string connectionString, T obj)
        {
            if (obj == null)
                return;

            var propertyContainer = ParseProperties(obj);

            var propertyName = propertyContainer.IdPairs.Keys.First();
            var propertyInfo = obj.GetType().GetProperty(propertyName);

            if (propertyInfo.GetValue(obj) == null)
                return;

            var sql = string.Format("DELETE [{0}] WHERE {1} = {2}",
                GetTableName<T>(),
                propertyName,
                propertyInfo.GetValue(obj));

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Execute(sql);
            }
        }

        /// <summary>
        /// Update an entity to db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="obj"></param>
        public static void Update<T>(string connectionString, T obj)
        {
            var propertyContainer = ParseProperties(obj);
            var setClause =
                string.Join(",", propertyContainer.ValuePairs.Select(
                    pair =>
                        string.Format("{0} = {1}", pair.Key,
                            pair.Value.ToSqlQueryCompatible())));

            var whereClause =
                string.Join(" AND ", propertyContainer.IdPairs.Select(
                    pair =>
                        string.Format("{0} = {1}", pair.Key,
                            pair.Value.ToSqlQueryCompatible())));

            var sql = string.Format(@"SET ANSI_NULLS OFF; UPDATE [{0}] SET {1} WHERE {2}; SET ANSI_NULLS ON;",
                typeof(T).Name, setClause, whereClause);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Execute(sql);
            }
        }

        /// <summary>
        /// Custom function for executed whatever you want
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="sql"></param>
        public static void Execute(string connectionString, string sql)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Execute(sql);
            }
        }

        /// <summary>
        /// Clear up all entity you created
        /// </summary>
        /// <param name="connectionString"></param>
        public static void ClearUpDbBasedOnCreation(string connectionString)
        {
            foreach (var obj in CreationBuilders)
            {
                var propertyContainer = ParseProperties(obj);

                var propertyName = propertyContainer.IdPairs.Keys.First();
                var propertyInfo = obj.GetType().GetProperty(propertyName);

                if (propertyInfo.GetValue(obj) == null)
                    return;

                var sql = string.Format("DELETE [{0}] WHERE {1} = {2}",
                    GetTableNameFor(obj.GetType()),
                    propertyName,
                    propertyInfo.GetValue(obj));

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Execute(sql);
                }
            }
        }

        /// <summary>
        /// Idealy, should rmb all declare to removed after the tests
        /// NOTE: Delete one by one manually first
        /// </summary>
        public static void ClearFactoryDefinitions()
        {
            Builders.Clear();
            CreationBuilders.Clear();
        }
    }
}
