using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mic.VetEducation.Repository
{
    public interface IRepository<T>
            where T : new()
    {
        string ConectionString { get; }
        IEnumerable<T> AsEnumerable();
        IEnumerable<T> Actives();
        T FirstOrDefault(int id);
        int Add(T model);
        void Update(int id, T model);
        void Delete(int id);
        void Restore(int id);
    }

    public class BaseRepository
    {

    }

    public class BaseRepository<T> : IRepository<T>
        where T : new()
    {
        public BaseRepository(string cs)
        {
            ConectionString = cs;
        }

        private string TableName => typeof(T).Name;

        public string ConectionString { get; }

        public IEnumerable<T> AsEnumerable()
        {
            return Execute($"Select * from {typeof(T).Name}");
        }
        public bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public int Add(T model)
        {
            var parameters = typeof(T).GetProperties()
                .Where(p => p.Name != "Id")
                .Select(p => new SqlParameter($"@{p.Name}", SqlHelper.GetDbType(p.PropertyType))
                {
                    Value = p.GetValue(model),
                    IsNullable = IsNullable(p.PropertyType)
                }).ToDictionary(p => p.ParameterName);

            if (parameters.TryGetValue("@CreatedOn", out var param))
            {
                param.Value = DateTime.Now;
            }

            if (parameters.TryGetValue("@ModifiedOn", out var param1))
            {
                param1.Value = DateTime.Now;
            }

            if (parameters.TryGetValue("@Active", out var param2))
            {
                param2.Value = true;
            }
            return Insert(parameters.Values.ToArray());
        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        public void Update(int id, T model)
        {
            PropertyInfo[] pi = typeof(T).GetProperties();

            int length = 0;

            for(int i = 1; i < pi.Length; i++)
            {
                if (GetPropValue(model, pi[i].Name) != default || GetPropValue(model, pi[i].Name) != null)
                {
                    length++;
                }
            }

            SqlParameter[] sp = new SqlParameter[length];

            int j = 0;

            for(int i = 1; i < pi.Length; i++)
            {
                if(GetPropValue(model, pi[i].Name) != null)
                {
                    sp[j] = new SqlParameter($"@{pi[i].Name}", pi[i].PropertyType) { Value = GetPropValue(model, pi[i].Name) };
                    j++;      
                }
            }

            Update(id, sp);
        }

        public void Delete(int id)
        {
            Update(id,
                new SqlParameter("@Active", SqlDbType.Bit) { Value = 0 },
                new SqlParameter("@ModifiedOn", SqlDbType.DateTime) { Value = DateTime.Now });
        }

        public void Restore(int id)
        {
            Update(id,
               new SqlParameter("@Active", SqlDbType.Bit) { Value = 1 },
               new SqlParameter("@ModifiedOn", SqlDbType.DateTime) { Value = DateTime.Now });
        }

        public IEnumerable<T> Actives()
        {
            return Execute($"Select * from {TableName} where Active = 1");
        }

        public T FirstOrDefault(int id)
        {
            return Execute($"Select * from {TableName} where Id = {id}").FirstOrDefault();
        }

        private IEnumerable<T> Execute(string query)
        {
            using var conection = new SqlConnection(ConectionString);

            conection.Open();

            using var cmd = new SqlCommand(query, conection);

            var reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                var props = typeof(T).GetProperties();

                while (reader.Read())
                {
                    var item = new T();

                    foreach (var prop in props)
                    {
                        var value = reader[prop.Name];
                        prop.SetValue(item, value == DBNull.Value ? null : value);
                    }

                    yield return item;
                }
            }
        }

        protected void ExecuteNonQuery(string query)
        {
            using var conection = new SqlConnection(ConectionString);

            conection.Open();

            using var cmd = new SqlCommand(query, conection);

            cmd.ExecuteNonQuery();
        }

        protected void Update(int id, params SqlParameter[] parameters)
        {
            using var conection = new SqlConnection(ConectionString);

            conection.Open();

            var queryBuilder = new StringBuilder();
            foreach (var param in parameters)
            {
                queryBuilder.Append(param.ParameterName.Substring(1)).Append("=").Append(param.ParameterName).Append(",");
            }

            string query = queryBuilder.ToString().TrimEnd(',');
            using var cmd = new SqlCommand($"UPDATE {TableName} Set {query} where Id = {id}", conection);
            cmd.Parameters.AddRange(parameters);

            cmd.ExecuteNonQuery();
        }

        //SELECT SCOPE_IDENTITY();

        protected int Insert(params SqlParameter[] parameters)
        {
            using var conection = new SqlConnection(ConectionString);

            conection.Open();

            var queryBuilder = new StringBuilder();
            foreach (var param in parameters)
            {
                queryBuilder.Append(param.ParameterName).Append(",");
            }

            string query = queryBuilder.ToString().TrimEnd(',');
            using var cmd = new SqlCommand($"Insert into {TableName} Values({query}); SELECT SCOPE_IDENTITY();", conection);
            cmd.Parameters.AddRange(parameters);
            //"@Name,@Surname,@BirthDay,@CreatedOn,@ModifiedOn,@Active"
            var value = cmd.ExecuteScalar();
            return (int)value;
        }


    }
}