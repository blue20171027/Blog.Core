using System.Collections.Generic;
using System.Threading.Tasks;
using Blog.Core.Common.HttpContextUser;
using Blog.Core.IServices;
using Blog.Core.Model;
using Blog.Core.Model.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Blog.Core.Common.Helper;

namespace Blog.Core.Controllers
{
    /// <summary>
    /// 角色管理
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Permissions.Name)]
    public class RoleController : ControllerBase
    {
        readonly IRoleServices _roleServices;
        readonly IUser _user;
        private readonly IUserRoleServices _userRoleServices;

        public RoleController(IRoleServices roleServices, IUser user, IUserRoleServices userRoleServices)
        {
            _roleServices = roleServices;
            _user = user;
            _userRoleServices = userRoleServices;
        }

        /// <summary>
        /// 获取全部角色
        /// </summary>
        /// <param name="page"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        // GET: api/User
        [HttpGet]
        public async Task<MessageModel<PageModel<Role>>> Get(int page = 1, int f = 0, string key = "")
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                key = "";
            }

            int intPageSize = 50;

            //var roleList = await _roleServices.QueryPage(a => a.IsDeleted != true && (a.Name != null && a.Name.Contains(key)), page, intPageSize, " Id desc ");
            var userRoleIds = (await _userRoleServices.Query(it => it.UserId == _user.ID && it.IsDeleted == false)).Select(it=>it.RoleId).ToList();
            PageModel<Role> roles;
            if (userRoleIds.Contains(1))
            {
                roles = await _roleServices.QueryPage(a => a.IsDeleted != true
                    && a.Pid == f && (key == "" || a.Name != null && a.Name.Contains(key)),
                    page, intPageSize, " Id desc ");
            }
            else
            {
                if (f > 0)
                {
                    roles = await _roleServices.QueryPage(a => a.IsDeleted != true
                        && a.Pid == f && (key == "" || a.Name != null && a.Name.Contains(key)),
                        page, intPageSize, " Id desc ");
                }
                else
                {
                    roles = await _roleServices.QueryPage(a => a.IsDeleted != true
                        && userRoleIds.Contains(a.Id) && (key == "" || a.Name != null && a.Name.Contains(key)),
                        page, intPageSize, " Id desc ");
                }
            }

            foreach (var item in roles.data)
            {
                List<int> pidarr = new List<int> { };
                var parent = await _roleServices.QueryById(item.Pid);

                while (parent != null)
                {
                    pidarr.Add(parent.Id);
                    parent = await _roleServices.QueryById(parent.Pid);
                }

                pidarr.Reverse();
                pidarr.Insert(0, 0);
                item.PidArr = pidarr;
                item.hasChildren = await _roleServices.ExistsChild(item.Id);
            }

            return new MessageModel<PageModel<Role>>()
            {
                msg = "获取成功",
                success = roles.dataCount >= 0,
                response = roles
            };
        }

        /// <summary>
        /// 获取角色树
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<RoleTree>> GetRoleTree()
        {
            var data = new MessageModel<RoleTree>();
            var roles = await _roleServices.Query(d => d.IsDeleted == false);
            var roleTrees = (from child in roles
                                   where child.IsDeleted == false
                                   orderby child.OrderSort
                                   select new RoleTree
                                   {
                                       value = child.Id,
                                       label = child.Name,
                                       pid = child.Pid,
                                       order = child.OrderSort,
                                   }).ToList();
            RoleTree rootRoot = new RoleTree
            {
                value = 0,
                pid = 0,
                label = "根节点"
            };
            RecursionHelper.LoopToAppendChildrenT(roleTrees, rootRoot);

            data.success = true;
            if (data.success)
            {
                data.response = rootRoot;
                data.msg = "获取成功";
            }

            return data;
        }

        /// <summary>
        /// 获取角色树
        /// </summary>
        /// <param name="hasCurrentRole">是否包含当前角色</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<List<RoleTree>>> GetCurrentUserRoleTree(bool hasCurrentRole = false)
        {
            var userRoles = await _userRoleServices.Query(it => it.UserId == _user.ID);
            var roleIds = userRoles.Select(it => it.RoleId).ToList();
            var data = new MessageModel<List<RoleTree>>();
            //超级管理员例外，可以操作所有角色。
            var allRoles = await _roleServices.Query(d => d.IsDeleted == false);
            var roleTrees = (from child in allRoles
                             where child.IsDeleted == false
                             orderby child.OrderSort
                             select new RoleTree
                             {
                                 value = child.Id,
                                 label = child.Name,
                                 pid = child.Pid,
                                 order = child.OrderSort,
                             }).ToList();
            var roleTreeList = new List<RoleTree>();
            if (roleIds.Contains(1))
            {
                roleIds = allRoles.Where(it => it.Pid == 0 && it.Id != 1).Select(it => it.Id).ToList();
            }
            roleIds.ForEach(id =>
            {
                RoleTree rootRoot = new RoleTree
                {
                    value = id,
                    pid = 0,
                    label = allRoles.Find(it => it.Id == id).Name
                };
                RecursionHelper.LoopToAppendChildrenT(roleTrees, rootRoot);
                roleTreeList.Add(rootRoot);
            });
            

            data.success = true;
            if (data.success)
            {
                data.response = roleTreeList;
                data.msg = "获取成功";
            }

            return data;
        }

        // GET: api/User/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
            return "value";
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        // POST: api/User
        [HttpPost]
        public async Task<MessageModel<string>> Post([FromBody] Role role)
        {
            var data = new MessageModel<string>();

            role.CreateId = _user.ID;
            role.CreateBy = _user.Name;

            var id = (await _roleServices.Add(role));
            data.success = id > 0;
            if (data.success)
            {
                data.response = id.ObjToString();
                data.msg = "添加成功";
            }

            return data;
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        // PUT: api/User/5
        [HttpPut]
        public async Task<MessageModel<string>> Put([FromBody] Role role)
        {
            var data = new MessageModel<string>();
            if (role != null && role.Id > 0)
            {
                data.success = await _roleServices.Update(role);
                if (data.success)
                {
                    data.msg = "更新成功";
                    data.response = role?.Id.ObjToString();
                }
            }

            return data;
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/ApiWithActions/5
        [HttpDelete]
        public async Task<MessageModel<string>> Delete(int id)
        {
            var data = new MessageModel<string>();
            if (id > 0)
            {
                var userDetail = await _roleServices.QueryById(id);
                userDetail.IsDeleted = true;
                data.success = await _roleServices.Update(userDetail);
                if (data.success)
                {
                    data.msg = "删除成功";
                    data.response = userDetail?.Id.ObjToString();
                }
            }

            return data;
        }
    }
}
