using Blog.Core.IRepository.UnitOfWork;
using Blog.Core.Model.Models;
using Blog.Core.Repository.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Core.Repository
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {

        }

        /// <summary>
        /// 通过用户id获取当前以及向上的所有角色
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        public async Task<List<Role>> PreviousRecursion(int userId)
        {
            var sql = @"with cte(id,pid,name, roleId) 
as 
(--下级父项 
select a.id, a.pid, a.name, a.id roleId from Role a 
	inner join UserRole b on b.RoleId = a.Id
	where b.UserId = @userId
union all 
--递归结果集中的父项 
select t.id,t.pid,t.name, c.roleId from Role as t 
inner join cte as c on t.id = c.pid 
) 
select id,pid,name, roleId from cte";
            var list = await Db.Ado.SqlQueryAsync<Role>(sql, new { userId });

            return list;
        }
    }
}
