var fullUrl = window.location.origin;
Ext.require([
    "Ext.tip.QuickTipManager",
    "Ext.menu.*",
    "Ext.form.field.ComboBox",
    "Ext.form.field.Text",
    "Ext.layout.container.Table",
    "Ext.container.ButtonGroup",
    "Ext.Img",
    "Ext.form.*",
    "Ext.grid.filters.Filters",
]);
Ext.onReady(function () {
    var token = localStorage.getItem('token');
    var searchPanel = Ext.create('Ext.form.Panel', {
        width: "100%",
        collapsible: true,
        overlapHeader: false,
        collapsed: true,//默认折叠
        collapseMode: 'header',
        titleCollapse: true,//隐藏title区域
        hideCollapseTool: false,//隐藏按钮
        split: true,
        collapseDirection: 'top',
        animCollapse: false,
        region: 'north',
        itmeID: 'searchid',
        autoHeight: true,
        renderTo: "toolbar",
        layout: "form", // 整个大的表单是form布局
        labelAlign: "left",
        bodyStyle: 'padding:0 5 5 5',
        border: 0,
        items: [{ // 行1
            layout: "column", // 从左往右的布局
            border: 0,
            items: [{
                border: 0,
                columnWidth: .18, // 该列有整行中所占百分比
                layout: "form", // 从上往下的布局
                items: [
                    {
                        xtype: "textfield",
                        id: "username",
                        fieldLabel: "角色编号",
                    }                    
                ]
            }, {
                    border: 0,
                    columnWidth: .19,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "name",
                        fieldLabel: "角色名称",
                    }]
                }
            ]
        }],
        buttonAlign: "center",
        buttons: [{
            text: '查询',
            formBind: true, //only enabled once the form is valid
            disabled: true,
            itemId: 'searchBtn',
            handler: function () {
                var form = this.up('form').getForm();
                if (form.isValid()) {
                    var queryJson = {
                        "username": Ext.getCmp("username").getValue(),
                        "name": Ext.getCmp("name").getValue(),
                    };
                    store.currentPage = 1;
                    store.proxy.extraParams = {
                        queryJson: JSON.stringify(queryJson)
                    };
                    store.load();
                }
            }
        }, {
            text: '重置',
            handler: function () {
                this.up('form').getForm().reset();
            }
        }]
    });
    // 定义数据模型
    Ext.define('OrderModel', {
        extend: 'Ext.data.Model',
        fields: [
            { name: 'importTime', type: 'date', dateFormat: 'Y-m-d H:i:s' },
            'customerName',
            'customerCode',
            'orderNumber',
            'orderSequence',
            { name: 'deliveryDate', type: 'date', dateFormat: 'Y-m-d' },
            'invoiceCreator',
            { name: 'createTime', type: 'date', dateFormat: 'Y-m-d H:i:s' }
        ]
    });

    // 创建数据存储
    var store = Ext.create('Ext.data.Store', {
        model: 'OrderModel',
        autoLoad: true,
        pageSize: 20,
        fields: ['inportdate', 'cusname', 'cuscode', 'ordercode', 'orderbin',
            'podate', 'createperson', 'createdate'],
        proxy: {
            type: 'ajax',
            url: fullUrl + '/Login/GetUserPageList',
            method: 'POST',  // 强制使用 POST
            actionMethods: {
                read: 'POST'
            },
            headers: {
                'Authorization': `Bearer ${token}`
            },
            paramsAsJson: true,
            reader: {
                type: 'json',
                rootProperty: 'data',
                totalProperty: 'total',
                transformResponse: function (rawResponse) {
                    try {
                        // 手动解析 JSON 字符串
                        const response = Ext.decode(rawResponse);
                        return response;
                    } catch (e) {
                        return { success: false, total: 0, data: [] };
                    }
                }
            },
            writer: {
                type: 'json'
            },
            listeners: {
                exception: function (proxy, response, operation) {
                    if (response.status == 401) {
                        parent.Ext.MessageBox.alert({
                            title: '警告',
                            message: '<div style="text-align:center;">请先登录账号</div>',
                            buttons: Ext.MessageBox.OK,
                            width: 100,
                            fn() {
                                localStorage.removeItem('token');
                                window.parent.location.href = "/login.html";
                            }
                        });
                    }
                }
            }
        }
    });
    // 单独增加按钮 方便后边优化按钮权限
    var gridBotton = [];
    gridBotton.push({
        text: '添加用户',
        handler: function () {
            var form = Ext.create('Ext.form.Panel', {
               // title: '添加新用户',
                border: false,
                width: 400,
                bodyPadding: 10,
                defaults: {
                    xtype: 'textfield',
                    allowBlank: false,  // 设置为必填
                    msgTarget: 'side',
                    width: '100%',
                    margin: '0 0 10 0'
                },
                items: [{
                    fieldLabel: '用户名',
                    name: 'username',
                    //id: 'username',
                    minLength: 3,
                    maxLength: 20,
                    regex: /^[a-zA-Z0-9_]+$/,
                    regexText: '只能包含字母、数字和下划线'
                }, {
                    fieldLabel: '密码',
                    name: 'password',
                    inputType: 'password',
                    //id: 'password',
                    minLength: 6,
                    maxLength: 20
                }, {
                    fieldLabel: '姓名',
                    name: 'name',
                    //id: 'name',
                    maxLength: 20
                }],
                buttons: [{
                    text: '提交',
                    handler: function () {
                        
                        if (form.getForm().isValid()) {
                            console.log(form.getForm().getValues())
                            var conn = new Ext.data.Connection();
                            conn.request({
                                url: fullUrl + '/Login/Adduser',
                                method: 'POST',
                                jsonData: form.getForm().getValues(),
                                binary: false, // 关键：告诉 ExtJS 处理二进制数据
                                timeout: 30000,
                                success: function (response) {
                                    console.log("responsesuccess", response);
                                    var resp = JSON.parse(response.responseText);
                                    if (!resp.success) {
                                        Ext.Msg.alert('提醒', "添加失败," + resp.mess);
                                    } else {
                                        form.up('window').close();

                                        Ext.getCmp('tableGrid').getStore().load();
                                    }

                                },
                                failure: function (response) {
                                    Ext.Msg.alert('错误', "添加失败");
                                }
                            });
                        }
                    }
                }, {
                    text: '取消',
                    handler: function () {
                        form.up('window').close();
                    }
                }]
            });

            Ext.create('Ext.window.Window', {
                title: '添加用户',
                height: 250,
                width: 450,
                layout: 'fit',
                items: [form]
            }).show();
        }
    });
    gridBotton.push({
        text: '修改密码',
        handler: function () {
            var checkedRecords = grid.getSelectionModel().getSelection();
            console.log('checkedRecords', checkedRecords);
            if (checkedRecords.length == 0) {
                Ext.Msg.alert('错误', "未选择数据");
                return;
            }
            var form = Ext.create('Ext.form.Panel', {
                // title: '添加新用户',
                border: false,
                width: 400,
                bodyPadding: 10,
                defaults: {
                    xtype: 'textfield',
                    allowBlank: false,  // 设置为必填
                    msgTarget: 'side',
                    width: '100%',
                    margin: '0 0 10 0'
                },
                items: [ {
                    fieldLabel: '密码',
                    name: 'password',
                    inputType: 'password',
                    id: 'password',
                    minLength: 6,
                    maxLength: 20
                }, {
                    fieldLabel: '再次输入',
                    name: 'name',
                    inputType: 'password',
                    id: 'password2',
                    minLength: 6,
                    maxLength: 20,
                    validator: function (value) {
                        var password = Ext.getCmp('password').getValue();
                        return (value === password) || '两次输入的密码不一致';
                    }
                }],
                buttons: [{
                    text: '提交',
                    handler: function () {

                        if (form.getForm().isValid()) {
                            if (Ext.getCmp('password').getValue() !==
                                Ext.getCmp('password2').getValue()) {
                                Ext.Msg.alert('错误', '两次输入的密码不一致');
                                return;
                            }
                            
                            var data = {
                                password: Ext.getCmp('password2').getValue(),
                                id: checkedRecords[0].data.id
                            }
                            console.log('data', data)
                            var conn = new Ext.data.Connection();
                            conn.request({
                                url: fullUrl + '/Login/pudatepswd',
                                method: 'POST',
                                jsonData: data,
                                binary: false, // 关键：告诉 ExtJS 处理二进制数据
                                timeout: 30000,
                                success: function (response) {
                                    console.log("responsesuccess", response);
                                    var resp = JSON.parse(response.responseText);
                                    if (!resp.success) {
                                        Ext.Msg.alert('提醒', "修改失败," + resp.mess);
                                    } else {
                                        form.up('window').close();
                                        Ext.Msg.alert('成功', "操作成功,");
                                    }

                                },
                                failure: function (response) {
                                    Ext.Msg.alert('错误', "修改失败");
                                }
                            });
                        }
                    }
                }, {
                    text: '取消',
                    handler: function () {
                        form.up('window').close();
                    }
                }]
            });

            Ext.create('Ext.window.Window', {
                title: '修改密码',
                height: 150,
                width: 350,
                layout: 'fit',
                items: [form]
            }).show();
        }
    });
    gridBotton.push({
        text: '删除',
        handler: function () {
            var checkedRecords = grid.getSelectionModel().getSelection();
            console.log('checkedRecords', checkedRecords);
            if (checkedRecords.length == 0) {
                Ext.Msg.alert('错误', "未选择行数据");
                return;
            }
            var conn = new Ext.data.Connection();
            conn.request({
                url: fullUrl + '/Login/deluser',
                method: 'POST',
                jsonData: { id: checkedRecords[0].data.id },
                binary: false, // 关键：告诉 ExtJS 处理二进制数据
                timeout: 60000,
                success: function (response) {
                    var resp = JSON.parse(response.responseText);
                    if (!resp.success) {
                        Ext.Msg.alert('提醒', "删除失败," + resp.mess);
                    } else {
                        Ext.getCmp('tableGrid').getStore().load();
                        form.up('window').close();
                        
                    }

                },
                failure: function (response) {
                    Ext.Msg.alert('错误', "删除失败");
                }
            });
        }
    });
    Ext.tip.QuickTipManager.init(); // 初始化提示管理器

    // 创建Grid面板
    var grid = Ext.create('Ext.grid.Panel', {
        //renderTo: Ext.getBody(),
        store: store,
        id:'tableGrid',
        width: '500px',
        height: '500',
        // forceFit: true,
        renderTo: 'tableData',
        region: 'center',
        //selModel: Ext.create("Ext.selection.CheckboxModel", {
        //    injectCheckbox: 0, //checkbox位于哪一列，默认值为0
        //    mode: "single", //multi,simple,single；默认为多选multi
        //    checkOnly: false, //如果值为true，则只用点击checkbox列才能选中此条记录
        //    allowDeselect: false, //如果值true，并且mode值为单选（single）时，可以通过点击checkbox取消对其的选择
        //    enableKeyNav: true,
        //}),
        plugins: {
            ptype: 'gridfilters', // 必须添加的插件
            filtersText: '筛选',   // 筛选菜单显示文本
            showMenu: true        // 显示筛选菜单图标
        },
        columns: [

            {
                text: '账号', dataIndex: 'username', width: 140, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '名称', dataIndex: 'name', width: 120,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            

        ],
        dockedItems: [{
            xtype: 'pagingtoolbar',
            store: store,
            dock: 'bottom',
            displayInfo: true
        }],
        tbar: gridBotton

        
    });
    // 主界面布局
    Ext.create('Ext.container.Viewport', {
        layout: 'border',
        //autoScroll: true,
        items: [searchPanel, grid]
    });
    function addUser(user) {
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/Login/Adduser',
            method: 'POST',
            jsonData: user,
            //headers: {
            //    'Authorization': `Bearer ${token}`
            //},
            binary: false, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 30000,
            success: function (response) {
                console.log("responsesuccess", response);
                var resp = JSON.parse(response.responseText);
                if (!resp.success) {
                    Ext.Msg.alert('提醒', "添加失败," + resp.mess);
                }
                    
            },
            failure: function (response) {
                Ext.Msg.alert('错误', "添加失败");
            }
        });
    }
});