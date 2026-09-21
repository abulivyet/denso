var fullUrl = window.location.origin;
var tokenCheck = function () {

    var token = localStorage.getItem('token');
    var formData = {
        page: 1,
        start: 0,
        limit: 1
    };
    // 高老师写法登录后调一下后台接口 未获取到后台登录信息退出登录
    //Ext.Ajax.request({
    //    url: fullUrl+"/orderImport/GetPageList",
    //    method: "POST",
    //    actionMethods: {
    //        read: 'POST'
    //    },
    //    headers: {
    //        Authorization: "Bearer " + token,
    //    },
    //    paramsAsJson: true,
    //    jsonData: formData,
    //    success: function (response) {
    //    },
    //    failure: function (response) {
    //        if (response.status === 401) {
    //            localStorage.removeItem('token');
    //            window.location.href = "/login.html";
    //        }
    //    }
    //});
}