using Blog.Core.IRepository.Base;
using Blog.Core.Model.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Core.Repository
{
    public interface IRoleRepository : IBaseRepository<Role>//类名
    {
        /// <summary>
        /// 通过用户id获取当前以及向上的所有角色
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        Task<List<Role>> PreviousRecursion(int userId);
    }
}
