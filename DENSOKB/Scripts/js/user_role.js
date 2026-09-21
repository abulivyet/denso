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
            { name: 'deliveryDate', type: 'date', dateFormat: 'Y-m-d' },
            { name: 'createTime', type: 'date', dateFormat: 'Y-m-d H:i:s' },
            { name: 'userIds', type: 'auto' }
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
            url: fullUrl + '/Role/GetRolePageList',
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
        text: '添加角色',
        handler: function () {
            var form = Ext.create('Ext.form.Panel', {
                // title: '添加新用户',
                border: false,
                width: 400,
                bodyPadding: 10,
                defaults: {
                    xtype: 'textfield',
        
                    msgTarget: 'side',
                    width: '100%',
                    margin: '0 0 10 0'
                },
                items: [{
                    fieldLabel: '角色编码',
                    name: 'r_code',
                    id: 'r_code',
                    minLength: 2,
                    maxLength: 20,
                    allowBlank: false,  // 显式设置为非必填
                }, {
                    fieldLabel: '角色名称',
                    name: 'r_name',
                    id: 'r_name',
                    allowBlank: false,  // 显式设置为非必填
                    minLength: 2,
                    maxLength: 20
                }, {
                    fieldLabel: '描述',
                    name: 'r_mark',
                    id: 'r_mark',
                    maxLength: 255
                }],
                buttons: [{
                    text: '提交',
                    handler: function () {
                        if (form.getForm().isValid()) {
                            console.log(form.getForm().getValues())
                            var conn = new Ext.data.Connection();
                            conn.request({
                                url: fullUrl + '/Role/Addrole',
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
                title: '添加角色',
                height: 250,
                width: 450,
                layout: 'fit',
                items: [form]
            }).show();
        }
    });
    // ============= 新增：分配用户按钮 =============
    gridBotton.push({
        text: '分配用户',
        handler: function () {
            var selectedRole = grid.getSelectionModel().getSelection();
            if (selectedRole.length === 0) {
                Ext.Msg.alert('提示', '请先选择一个角色');
                return;
            }

            var roleId = selectedRole[0].data.id; // 获取选中的角色ID

            // 获取已分配的用户ID数组
            var allocatedUserIds = []; // 示例，实际应从服务器获取
            var conn = new Ext.data.Connection();
            conn.request({
                url: fullUrl + '/Role/getAllUser',
                method: 'POST',
                jsonData: { roleId: roleId },
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                binary: true, // 关键：告诉 ExtJS 处理二进制数据
                timeout: 300000,
                success: function (response) {
                },
                failure: function (response) {
                    console.log("responsefailure", response)
                    mask.hide();
                    Ext.Msg.alert('错误', response.responseText || "请求失败");
                }
            });
            // 创建一个用户列表窗口
            Ext.create('Ext.window.Window', {
                title: '分配用户给角色',
                width: 600,
                height: 400,
                layout: 'fit',
                items: {
                    xtype: 'gridpanel',
                    store: Ext.create('Ext.data.Store', {
                        model: 'OrderModel',
                        autoLoad: true,
                        // 这里应该加载所有用户的列表数据
                        fields: ['id', 'name', { name: 'allocated', type: 'boolean' } // 添加布尔类型字段
                        ],
                        autoLoad: true,
                        proxy: {
                            type: 'ajax',
                            url: fullUrl + '/Role/getAllUser', // 假设获取所有用户的接口
                            method: 'POST',
                            actionMethods: {
                                read: 'POST'
                            },
                            headers: { 'Authorization': `Bearer ${token}` },
                            paramsAsJson: true,
                            reader: {
                                type: 'json',
                                rootProperty: 'data'
                            },
                            writer: {
                                type: 'json'
                            },
                           
                        },
                        listeners: {
                            load: function (store, records) {
                                Ext.each(records, function (record) {
                                    // 如果当前用户ID在分配列表中，则设置为已选中
                                    if (allocatedUserIds.includes(record.get('id'))) {
                                        record.set('allocated', true);
                                    }
                                });
                            }
                        }
                    }),
                    columns: [
                        { text: '用户ID', dataIndex: 'id', width: 100 },
                        { text: '用户名', dataIndex: 'name', width: 200 },
                        {
                            xtype: 'checkcolumn',
                            text: '分配',
                            dataIndex: 'allocated',
                            width: 80,
                            listeners: {
                                checkchange: function (checker, rowIndex, checked, eOpts) {

                                    var record = checker.up('grid').getStore().getAt(rowIndex);
                                    //console.log("id", record);
                                    if (checked) {
                                        // 如果选中，则添加到allocatedUserIds中（如果还未存在）
                                        if (!allocatedUserIds.includes(record.data.id)) {
                                            allocatedUserIds.push(record.data.id);
                                        }
                                    } else {
                                        // 如果取消选中，则从allocatedUserIds中移除
                                        var index = allocatedUserIds.indexOf(record.data.id);
                                        if (index !== -1) {
                                            allocatedUserIds.splice(index, 1);
                                        }
                                    }
                                }
                            }
                        }
                    ],
                    tbar: [{
                        text: '保存分配',
                        handler: function () {
                            console.log("选中的数据", allocatedUserIds)
                            // 发送请求到服务器保存分配的用户
                            var conn = new Ext.data.Connection();
                            conn.request({
                                url: fullUrl + '/Role/SetUserRole',
                                method: 'POST',
                                jsonData: { roleId: roleId, userIds: allocatedUserIds },
                                //binary: false, // 关键：告诉 ExtJS 处理二进制数据
                                headers: { 'Content-Type': 'application/json' },
                                success: function (response) {
                                    var resp = JSON.parse(response.responseText);
                                    if (resp.success) {
                                        Ext.Msg.alert('成功', '用户分配成功');
                                        grid.getStore().load(); // 刷新角色网格数据
                                    } else {
                                        Ext.Msg.alert('错误', '用户分配失败: ' + resp.mess);
                                    }
                                },
                                failure: function (response) {
                                    Ext.Msg.alert('错误', '请求失败');
                                }
                            });
                            this.up('window').close();
                        }
                    }]
                }
            }).show();
        }
    });
    // ============= 新增结束 =============
    gridBotton.push({
        text: '删除角色',
        handler: function () {
            var checkedRecords = grid.getSelectionModel().getSelection();
            console.log('checkedRecords', checkedRecords);
            if (checkedRecords.length == 0) {
                Ext.Msg.alert('错误', "未选择行数据");
                return;
            }
            var conn = new Ext.data.Connection();
            conn.request({
                url: fullUrl + '/Role/deluser',
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
        id: 'tableGrid',
        width: '500px',
        height: '500',
        // forceFit: true,
        renderTo: 'tableData',
        region: 'center',
        plugins: {
            ptype: 'gridfilters', // 必须添加的插件
            filtersText: '筛选',   // 筛选菜单显示文本
            showMenu: true        // 显示筛选菜单图标
        },
        columns: [

            {
                text: '角色编码', dataIndex: 'r_code', width: 140, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '角色名称', dataIndex: 'r_name', width: 120,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
        {
            text: '描诉', dataIndex: 'r_mark', width: 120,
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

});