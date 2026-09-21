Ext.onReady(onDownloadRows);
var user = localStorage.getItem('user');
var itemData;
var fullUrl = window.location.origin;
//新建tabpanel
var tab = Ext.create('Ext.TabPanel', {
    region: 'center',
    deferredRender: false,
    activeTab: 0,
    resizeTabs: true, // turn on tab resizing
    minTabWidth: 115,
    tabWidth: 135,
    id: "mainTabPanel",
    enableTabScroll: true
});
function onDownloadRows() {  
    var username = localStorage.user;
    console.log("当前用户", username)
    var conn = new Ext.data.Connection();
    conn.request({
        url: fullUrl + '/role/GetRoleMenu?user=' + username,
        method: 'GET',
        jsonData: username,
        headers: {
            'Content-Type': null
        },
        timeout: 300000,
        success: function (response) {
            console.log("后台返回数据response", response);
            var menuPanels = JSON.parse(response.responseText);
            console.log("获取角色权限菜单itemData", menuPanels);
            //var tab = Ext.getCmp('mainTabPanel');
            itemData = menuPanels.map(function (panel) {
                return Ext.apply(panel, {
                    listeners: {
                        itemclick: function (v, r, item) {
                            console.log("点击事件", r);
                            if (r.raw.url == "" || r.raw.url == undefined) {
                                return;
                            }
                            var n = tab.getComponent(r.raw.id);
                            if (r.raw.id === 'root') {
                                return;
                            }

                            // 检查标签页是否已存在
                            if (!n) { // 判断是否已经打开该面板
                                n = tab.add({
                                    'id': r.raw.id,
                                    'title': r.raw.text,
                                    closable: true, // 通过html载入目标页
                                    html: '<iframe scrolling="auto" frameborder="0" width="100%" height="100%" src="' + r.raw.url + '"></iframe>'
                                });
                            }
                            tab.setActiveTab(n);
                        }
                    }
                });
            });
            console.log("itemData", itemData);
            panelLayout();
        },
        failure: function (response) {
            Ext.Msg.alert('错误', '获取菜单失败');
            panelLayout();
        }
    });
}
function panelLayout() {

    var viewPort = Ext.create('Ext.Viewport', {
        layout: "border",
        items: [{
            region: 'north',
            minHeight: 68,
            html: '<img src="/images/logo.png" height="60px"> '
                + '<div class="toptitle">南沙电装OMS系统</div>'
                + '<div class="userinfo">当前用户：<span id="userInfo_Name">' + user + '</span></div>'
                + '<div class="logout">退出登录<span id="login_out"></span></div>',
            listeners: {
                afterrender: function (panel) {
                    // 选择所有.my-btn元素并绑定点击事件
                    Ext.select('.logout', panel.getEl()).on('click', function (e, target) {
                        Ext.Msg.confirm('确认', '确定要退出系统吗？', function (btn) {
                            if (btn === 'yes') {
                                localStorage.removeItem('token');
                                window.parent.location.href = "/login.html";
                            }
                        });
                    });
                }
            }
        }, {
            title: 'West Region is collapsible',
            region: 'west',
            xtype: 'panel',
            width: 200,
            collapsible: true, // 设置可折叠
            id: 'west-region-container',
            layout: 'fit',
            margins: '0 0 0 0',
            layout: 'accordion',
            title: '菜单',
            split: true,
            collapsible: true,
            layoutConfig: {
                animate: true
            },
            items: itemData ,

        }, tab, {
            region: 'south',
            minHeight: 40,
            html: "<div align='center' style='line-height:40px;'>版权所有  大宇宙信息创造（中国）有限公司</div>"
        }]
    })
};