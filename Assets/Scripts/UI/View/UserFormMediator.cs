﻿/* 
    作者：Sheh伟伟
    博客：http://www.jianshu.com/users/fd3eec0ab0f2/latest_articles
    Github：https://github.com/DavidSheh
*/

using System;
using System.Collections.Generic;

using PureMVC.Patterns;
using PureMVC.Interfaces;

using Demo.PureMVC.EmployeeAdmin.Model;
using Demo.PureMVC.EmployeeAdmin.Model.VO;
using Demo.PureMVC.EmployeeAdmin.View.Components;

namespace Demo.PureMVC.EmployeeAdmin.View
{
    public class UserFormMediator : Mediator, IMediator
    {
        private UserProxy userProxy;

        public new const string NAME = "UserFormMediator";

        public UserFormMediator(UserForm viewComponent)
            : base(NAME, viewComponent)
        {
            UserForm.AddUser += UserForm_AddUser;
            UserForm.UpdateUser += UserForm_UpdateUser;
            UserForm.CancelUser += UserForm_CancelUser;
        }

        public override void OnRegister()
        {
            base.OnRegister();
            userProxy = (UserProxy)Facade.RetrieveProxy(UserProxy.NAME);
        }

        private UserForm UserForm
        {
            get { return (UserForm)ViewComponent; }
        }

        void UserForm_AddUser()
        {
            UserVO user = UserForm.User;
            userProxy.AddItem(user);
            SendNotification(NotificationConst.USER_ADDED, user);
            UserForm.ClearForm();
        }

        void UserForm_UpdateUser()
        {
            UserVO user = UserForm.User;
            userProxy.UpdateItem(user);
            SendNotification(NotificationConst.USER_UPDATED, user);
            UserForm.ClearForm();
        }

        void UserForm_CancelUser()
        {
            SendNotification(NotificationConst.CANCEL_SELECTED);
            UserForm.ClearForm();
        }

        public override IList<string> ListNotificationInterests()
        {
            IList<string> list = new List<string>();
            list.Add(NotificationConst.NEW_USER);
            list.Add(NotificationConst.USER_DELETED);
            list.Add(NotificationConst.USER_SELECTED);
            return list;
        }

        public override void HandleNotification(INotification note)
        {
            UserVO user;

            switch (note.Name)
            {
                case NotificationConst.NEW_USER:
                    user = (UserVO)note.Body;
                    UserForm.ShowUser(user, UserFormMode.ADD);
                    break;

                case NotificationConst.USER_DELETED:
                    UserForm.ClearForm();
                    break;

                case NotificationConst.USER_SELECTED:
                    user = (UserVO)note.Body;
                    UserForm.ShowUser(user, UserFormMode.EDIT);
                    break;
            }
        }
    }
}
