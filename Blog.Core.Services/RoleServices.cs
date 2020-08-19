using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.Model.Models;
using Blog.Core.Repository;
using Blog.Core.Services.BASE;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Core.Services
{
    /// <summary>
    /// RoleServices
    /// </summary>	
    public class RoleServices : BaseServices<Role>, IRoleServices
    {

        IRoleRepository _dal;
        private readonly IUserRoleServices userRoleServices;

        public RoleServices(IRoleRepository dal, IUserRoleServices userRoleServices)
        {
            this._dal = dal;
            this.userRoleServices = userRoleServices;
            base.BaseDal = dal;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="roleName"></param>
       /// <returns></returns>
        public async Task<Role> SaveRole(string roleName)
        {
            Role role = new Role(roleName);
            Role model = new Role();
            var userList = await base.Query(a => a.Name == role.Name && a.Enabled);
            if (userList.Count > 0)
            {
                model = userList.FirstOrDefault();
            }
            else
            {
                var id = await base.Add(role);
                model = await base.QueryById(id);
            }

            return model;

        }

        [Caching(AbsoluteExpiration = 30)]
        public async Task<string> GetRoleNameByRid(int rid)
        {
            return ((await base.QueryById(rid))?.Name);
        }

        /// <summary>
        /// 是否存在子类
        /// </summary>
        /// <param name="id">当前id</param>
        /// <returns></returns>
        public async Task<bool> ExistsChild(int id)
        {
            var count = await _dal.QueryCount(it => it.Pid == id && it.IsDeleted != true);

            return count > 0;
        }

        /// <summary>
        /// 获取某个用户的所有上级角色
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        public async Task<List<List<int>>> GetPreviousRoleIds(int userId)
        {
            var list = await _dal.PreviousRecursion(userId);
            var ridArray = list.GroupBy(it=>it.RoleId).Select(it => it.Select(a=>a.Id).ToList()).ToList();

            return ridArray;
        }

        /// <summary>
        /// 获取某个用户的所有下级角色
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        public async Task<List<Role>> GetNextRoles(int userId)
        {
            var userRoleList = await userRoleServices.Query(it => it.UserId == userId && it.IsDeleted == false);
            var roleList = new List<Role>();
            var allRoleList = await _dal.Query(it=>it.IsDeleted == false);
            userRoleList.ForEach(it=> 
            {
                var role = allRoleList.Find(a=>a.Id == it.RoleId);
                RecursionHelper.LoopToAppendList(allRoleList, role, roleList);
            });

            return roleList;
        }
    }
}
