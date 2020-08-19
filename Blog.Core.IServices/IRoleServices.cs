using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Core.IServices
{	
	/// <summary>
	/// RoleServices
	/// </summary>	
    public interface IRoleServices :IBaseServices<Role>
	{
        Task<Role> SaveRole(string roleName);
        Task<string> GetRoleNameByRid(int rid);
        /// <summary>
        /// �Ƿ��������
        /// </summary>
        /// <param name="id">��ǰid</param>
        /// <returns></returns>
        Task<bool> ExistsChild(int id);
        /// <summary>
        /// ��ȡĳ���û��������ϼ���ɫ
        /// </summary>
        /// <param name="userId">�û�id</param>
        /// <returns></returns>
        Task<List<List<int>>> GetPreviousRoleIds(int userId);
        /// <summary>
        /// ��ȡĳ���û��������¼���ɫ
        /// </summary>
        /// <param name="userId">�û�id</param>
        /// <returns></returns>
        Task<List<Role>> GetNextRoles(int userId);
    }
}
