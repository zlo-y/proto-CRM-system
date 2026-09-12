using Microsoft.AspNetCore.Identity;
using Manager.DataAccess.Entities;
using Moq;

namespace Manager.BusinessLogic.Tests.TestHelpers;

public static class MockUserManagerFactory
{
    public static Mock<UserManager<User>> Create()
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mgr;
    }
}