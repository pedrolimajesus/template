using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shrike.Dal.Tests
{
    using System.Linq;

    using Shrike.DAL.Manager;
    using Shrike.Dal.Tests.Context;

    [TestClass]
    public class UserManagerTest
    {
        [TestMethod]
        public void GetAllValidUsersTest()
        {
            TenancyContextProvider.Tenancy = DefaultRoles.SuperAdmin;
            var userManager = new UserManager();
            var users = userManager.GetAllValidUsers();

            Assert.IsNotNull(users);
            Assert.AreEqual(users.Count(), 1);
            Assert.AreEqual(
                users.First().AppUser.AccountRoles.First().ToLowerInvariant(), "authorization/roles/administrator");

            TenancyContextProvider.Tenancy = "Tenancy33";

            users = userManager.GetAllValidUsers();
            Assert.IsNotNull(users);
            Assert.AreEqual(users.Count(), 2);

            bool found = false;
            foreach (var user in users)
            {
                found = user.AppUser.AccountRoles.Contains("authorization/roles/user");
                if (found) break;
            }

            Assert.IsTrue(found);
        }
    }
}