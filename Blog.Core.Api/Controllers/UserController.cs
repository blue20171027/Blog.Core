using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blog.Core.AuthHelper.OverWrite;
using Blog.Core.Common.Helper;
using Blog.Core.Common.HttpContextUser;
using Blog.Core.IRepository.UnitOfWork;
using Blog.Core.IServices;
using Blog.Core.Model;
using Blog.Core.Model.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Blog.Core.Controllers
{
    /// <summary>
    /// 用户管理
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Permissions.Name)]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        readonly ISysUserInfoServices _sysUserInfoServices;
        readonly IUserRoleServices _userRoleServices;
        readonly IRoleServices _roleServices;
        private readonly IUser _user;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="sysUserInfoServices"></param>
        /// <param name="userRoleServices"></param>
        /// <param name="roleServices"></param>
        /// <param name="user"></param>
        /// <param name="logger"></param>
        public UserController(IUnitOfWork unitOfWork, ISysUserInfoServices sysUserInfoServices, IUserRoleServices userRoleServices, 
            IRoleServices roleServices, IUser user, ILogger<UserController> logger)
        {
            _unitOfWork = unitOfWork;
            _sysUserInfoServices = sysUserInfoServices;
            _userRoleServices = userRoleServices;
            _roleServices = roleServices;
            _user = user;
            _logger = logger;
        }

        /// <summary>
        /// 获取全部用户
        /// </summary>
        /// <param name="page"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        // GET: api/User
        [HttpGet]
        public async Task<MessageModel<PageModel<sysUserInfo>>> Get(int page = 1, string key = "")
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                key = "";
            }
            int intPageSize = 50;
            var nextRoleIds = (await _roleServices.GetNextRoles(_user.ID)).Select(it=>it.Id).ToList();
            var data = await _sysUserInfoServices.QueryTabsPage<sysUserInfo, UserRole, sysUserInfo>((a, b) => 
                new object[]
                {
                    JoinType.Inner, a.uID == b.UserId
                },
                (a, b) => a.tdIsDelete != true && a.uStatus >= 0 && ((a.uLoginName != null && a.uLoginName.Contains(key)) || (a.uRealName != null && a.uRealName.Contains(key))
                && nextRoleIds.Contains(b.RoleId)),
                a => new { a.uID },
                (a, b) => a,
                page, intPageSize, " uID desc ");

            #region MyRegion

            // 这里可以封装到多表查询，此处简单处理
            var userIds = data.data.Select(it => it.uID).ToList();
            var allUserRoles = await _userRoleServices.Query(d => d.IsDeleted == false && userIds.Contains(d.UserId));
            var allRoles = await _roleServices.Query(d => d.IsDeleted == false);

            var currentRoleId = (await _userRoleServices.Query(it => it.UserId == _user.ID && it.IsDeleted == false)).FirstOrDefault();
            var sysUserInfos = data.data;
            foreach (var item in sysUserInfos)
            {

                item.RIDArray = await _roleServices.GetPreviousRoleIds(item.uID);
                var userRoleIds = allUserRoles.Where(d => d.UserId == item.uID).Select(d => d.RoleId).ToList();
                List<List<int>> roleIds = new List<List<int>>();
                foreach (var roleId in userRoleIds)
                {
                    List<int> ids = new List<int>();
                    var role = allRoles.Where(it => it.Id == roleId).FirstOrDefault();
                    if (role == null) continue;
                    ids.Add(role.Id);
                    Role parent = allRoles.Where(it => it.Id == role.Pid).FirstOrDefault();
                    if (parent != null)
                    {
                        ids.Add(parent.Id);
                    }
                    while (parent != null)
                    {
                        parent = allRoles.Where(it => it.Id == parent.Pid).FirstOrDefault();
                        if (parent != null)
                        {
                            ids.Add(parent.Id);
                        }
                    };
                    ids.Reverse();
                    roleIds.Add(ids);
                }
                item.RIDs = new List<int>();
                item.RIDArray = roleIds;
                item.RoleNames = allRoles.Where(d => userRoleIds.Contains(d.Id)).Select(d => d.Name).ToList();
            }

            data.data = sysUserInfos;
            #endregion


            return new MessageModel<PageModel<sysUserInfo>>()
            {
                msg = "获取成功",
                success = data.dataCount >= 0,
                response = data
            };

        }

        // GET: api/User/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public string Get(string id)
        {
            _logger.LogError("test wrong");
            return "value";
        }

        // GET: api/User/5
        /// <summary>
        /// 获取用户详情根据token
        /// 【无权限】
        /// </summary>
        /// <param name="token">令牌</param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<MessageModel<sysUserInfo>> GetInfoByToken(string token)
        {
            var data = new MessageModel<sysUserInfo>();
            if (!string.IsNullOrEmpty(token))
            {
                var tokenModel = JwtHelper.SerializeJwt(token);
                if (tokenModel != null && tokenModel.Uid > 0)
                {
                    var userinfo = await _sysUserInfoServices.QueryById(tokenModel.Uid);
                    if (userinfo != null)
                    {
                        data.response = userinfo;
                        data.success = true;
                        data.msg = "获取成功";
                    }
                }

            }
            return data;
        }

        /// <summary>
        /// 添加一个用户
        /// </summary>
        /// <param name="sysUserInfo"></param>
        /// <returns></returns>
        // POST: api/User
        [HttpPost]
        public async Task<MessageModel<string>> Post([FromBody] sysUserInfo sysUserInfo)
        {
            var data = new MessageModel<string>();

            sysUserInfo.uLoginPWD = MD5Helper.MD5Encrypt32(sysUserInfo.uLoginPWD);
            sysUserInfo.uRemark = _user.Name;

            var id = await _sysUserInfoServices.Add(sysUserInfo);
            data.success = id > 0;
            if (data.success)
            {
                data.response = id.ObjToString();
                data.msg = "添加成功";
            }

            return data;
        }

        /// <summary>
        /// 更新用户与角色
        /// </summary>
        /// <param name="sysUserInfo"></param>
        /// <returns></returns>
        // PUT: api/User/5
        [HttpPut]
        public async Task<MessageModel<string>> Put([FromBody] sysUserInfo sysUserInfo)
        {
            // 这里使用事务处理

            var data = new MessageModel<string>();
            try
            {
                _unitOfWork.BeginTran();

                if (sysUserInfo != null && sysUserInfo.uID > 0)
                {
                    if (sysUserInfo.RIDs.Count > 0)
                    {
                        // 无论 Update Or Add , 先删除当前用户的全部 U_R 关系
                        var usreroles = (await _userRoleServices.Query(d => d.UserId == sysUserInfo.uID)).Select(d => d.Id.ToString()).ToArray();
                        if (usreroles.Count() > 0)
                        {
                            var isAllDeleted = await _userRoleServices.DeleteByIds(usreroles);
                        }

                        // 然后再执行添加操作
                        var userRolsAdd = new List<UserRole>();
                        sysUserInfo.RIDs.ForEach(rid =>
                       {
                           userRolsAdd.Add(new UserRole(sysUserInfo.uID, rid));
                       });

                        await _userRoleServices.Add(userRolsAdd);

                    }

                    data.success = await _sysUserInfoServices.Update(sysUserInfo);

                    _unitOfWork.CommitTran();

                    if (data.success)
                    {
                        data.msg = "更新成功";
                        data.response = sysUserInfo?.uID.ObjToString();
                    }
                }
            }
            catch (Exception e)
            {
                _unitOfWork.RollbackTran();
                _logger.LogError(e, e.Message);
            }

            return data;
        }

        /// <summary>
        /// 删除用户
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
                var userDetail = await _sysUserInfoServices.QueryById(id);
                userDetail.tdIsDelete = true;
                data.success = await _sysUserInfoServices.Update(userDetail);
                if (data.success)
                {
                    data.msg = "删除成功";
                    data.response = userDetail?.uID.ObjToString();
                }
            }

            return data;
        }
    }
}
