using System.Collections.Generic;
using System.Data.SqlClient;
using Dapper;
using Dapper.Contrib.Extensions;
using sg.com.titansoft.TiDALBase.Entity;
using sg.com.titansoft.TiDALBase.Exception;
using sg.com.titansoft.TiDALBase.Repository;

namespace FactoryGirlExt
{
    public class DbRespository : TiReadOnlyRepositoryBase<TiDbContext>
    {
        private readonly TiDbContext _DbContext;

        public DbRespository(TiDbContext dbContext)
            : base(dbContext)
        {
            _DbContext = dbContext;
        }

        public void Exec(string sql)
        {

            Execute(c => c.Execute(sql));
        }

        public T Get<T>(int id) where T : class
        {
            SqlConnection connection = DbContext.OpenConnection();
            try
            {
                return connection.Get<T>(id);
            }
            catch (System.Exception ex)
            {
                throw new TiDalException(string.Format("TiDal.Base::sp failed: [{0}]", "Get<T>(" + id + ")"), ex);
            }
            finally
            {
                DbContext.ReleaseConnection(connection);
            }
        }

        public IEnumerable<dynamic> Query(string sql)
        {
            SqlConnection connection = DbContext.OpenConnection();
            try
            {
                return connection.Query<dynamic>(sql);
            }
            catch (System.Exception ex)
            {
                throw new TiDalException(string.Format("TiDal.Base::sp failed: [{0}]", "Query(" + sql + ")"), ex);
            }
            finally
            {
                DbContext.ReleaseConnection(connection);
            }
        }

        public IEnumerable<dynamic> Query(string sql, object param)
        {
            SqlConnection connection = DbContext.OpenConnection();
            try
            {
                return connection.Query(sql, param);
            }
            catch (System.Exception ex)
            {
                throw new TiDalException(string.Format("TiDal.Base::sp failed: [{0}]", "Query(" + sql + ")"), ex);
            }
            finally
            {
                DbContext.ReleaseConnection(connection);
            }
        }
    }
}
