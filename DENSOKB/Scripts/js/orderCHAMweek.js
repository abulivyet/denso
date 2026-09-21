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
            items: [
                {
                border: 0,
                columnWidth: .18, // 该列有整行中所占百分比
                layout: "form", // 从上往下的布局
                items: [
                    {
                        xtype: "textfield",
                        id: "ord_status",
                        fieldLabel: "订单状态",
                    }
                ]
            }, {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "orderCode",
                        fieldLabel: "订单号",
                    }
                    ]
                }, {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "court_summons",
                        fieldLabel: "传票号",
                    }
                    ]
                },
                {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "material_code",
                        fieldLabel: "物料代码",
                    }
                    ]
                },
                {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "project_code",
                        fieldLabel: "计划代码",
                    }
                    ]
                },{
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: 'datefield',
                        id: "sendStart",
                        format: 'Y-m-d',
                        editable: false,
                        fieldLabel: "发货日期",
                    }
                    ]
                },{
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: 'datefield',
                        id: "sendEnd",
                        format: 'Y-m-d',
                        editable: false,
                        fieldLabel: "至",
                    }
                    ]
                }

            ],
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
                        "ord_status": Ext.getCmp("ord_status").getValue(),//订单状态
                        "orderCode": Ext.getCmp("orderCode").getValue(),//订单号
                        "court_summons": Ext.getCmp("court_summons").getValue(), // 传票号
                        "material_code": Ext.getCmp("material_code").getValue(),// 物料代码
                        "project_code": Ext.getCmp("project_code").getValue(),// 计划代码
                        "sendStart": Ext.Date.format(Ext.getCmp("sendStart").getValue(), 'Y-m-d'),
                        "sendEnd": Ext.Date.format(Ext.getCmp("sendEnd").getValue(), 'Y-m-d')
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
        },{
            text: '下载',
            handler: function(){
                var cuscode = "CHAM"
                var startdate = Ext.Date.format(Ext.getCmp("sendStart").getValue(), 'Y-m-d');
                var enddate = Ext.Date.format(Ext.getCmp("sendEnd").getValue(), 'Y-m-d');

                if (!cuscode) {
                    Ext.Msg.alert('提示', '客户类别不能为空');
                    return;
                }

                if (!startdate) {
                    Ext.Msg.alert('提示', '发货开始日期不能为空');
                    return;
                }

                if (!enddate) {
                    Ext.Msg.alert('提示', '发货截止日期不能为空');
                    return;
                }

                var url = '/ExportRpt/getRpt?cuscode=' + encodeURIComponent(cuscode) +
                    '&startdate=' + encodeURIComponent(startdate) +
                    '&enddate=' + encodeURIComponent(enddate);
                var link = document.createElement('a');
                link.href = url;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            }
        }
        
        ]
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
            url: fullUrl + '/CHAMdispose/GetPageWeekList',
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
    var gridBotton = [];
    gridBotton.push({
        text: '删除',
        handler: function () {
            Ext.Msg.alert('确认操作', '是否确认删除', function (btnId) {
                if (btnId === 'ok') {
                    var checkedRecords = grid.getSelectionModel().getSelection();
                    console.log("选择的数据", checkedRecords)
                    //如果先择了选择记录下载，判断选择记录是否为空
                    if (checkedRecords.length == 0) {
                        parent.Ext.Msg.alert('提示', '请勾选数据');
                        return;
                    }
                    var myArray = [];
                    for (var i = 0; i < checkedRecords.length; i++) {

                        myArray.push(checkedRecords[i].id);


                    }

                    console.log(myArray)
                    deletePost(myArray);
                }
            });

        }
    },);
    Ext.tip.QuickTipManager.init(); // 初始化提示管理器
    // 创建Grid面板
    var grid = Ext.create('Ext.grid.Panel', {
        //renderTo: Ext.getBody(),
        id: 'chamOrderGrid',
        store: store,
        width: '500px',
        height: '500',
        // forceFit: true,
        renderTo: 'tableData',
        region: 'center',
        selModel: Ext.create("Ext.selection.CheckboxModel", {
            injectCheckbox: 0, //checkbox位于哪一列，默认值为0
            mode: "multi", //multi,simple,single；默认为多选multi
            checkOnly: false, //如果值为true，则只用点击checkbox列才能选中此条记录
            allowDeselect: true, //如果值true，并且mode值为单选（single）时，可以通过点击checkbox取消对其的选择
            enableKeyNav: true,
        }),
        plugins: {
            ptype: 'gridfilters', // 必须添加的插件
            filtersText: '筛选',   // 筛选菜单显示文本
            showMenu: true        // 显示筛选菜单图标
        },
        viewConfig: {
            enableTextSelection: true  // 关键配置  可复制文本
        },
        columns: [

            {
                text: '订单状态', dataIndex: 'ord_status', width: 140, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '订单号', dataIndex: 'orderCode', width: 120,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '发布日期', dataIndex: 'publishDate', width: 140,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '客户名称', dataIndex: 'cusname', width: 160,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '传票', dataIndex: 'court_summons', width: 130,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '物料编码', dataIndex: 'material_code', width: 140,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '物料名称', dataIndex: 'material_name', width: 150,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '计划代码', dataIndex: 'project_code', width: 110,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '需求日期', dataIndex: 'demanded_date', width: 120,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '收货时间', dataIndex: 'receiving_time', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '数量', dataIndex: 'quantity', width: 70,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '单位', dataIndex: 'unit', width: 70,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '订单类型', dataIndex: 'order_type', width: 160,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '收货地址', dataIndex: 'receiving_address', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '搬入场所代码', dataIndex: 'movingIn_code', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '新收荷日期', dataIndex: 'demanded_date_new', width: 100,
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

    function deletePost( rowdata) {
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/CHAMdispose/DeleteRecords',
            method: 'POST',
            jsonData: rowdata,
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                console.log(response);
                var grid = Ext.getCmp('chamOrderGrid'); 
                if (grid && grid.getStore()) {
                    grid.getStore().reload(); // 刷新表格数据
                }
            },
            failure: function (response) {
                Ext.Msg.alert('错误', '订单删除失败');
            }
        });
    }

    function onDownloadRows(rowdata) {  // 根据选择行下载
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/CHAMdispose/GetPageWeekList',
            method: 'POST',
            jsonData: JSON.stringify(rowdata),
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                console.log("response", response)
                DownloadResponse(response);
            },
            failure: function (response) {
                Ext.Msg.alert('错误', '文件下载失败');
            }
        });
    };
    function DownloadResponse(response) {
        var contentDisposition = response.getResponseHeader('Content-Disposition');
        // 2. 正则提取编码后的文件名
        var matches = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        var encodedFileName = matches[1].replace(/['"]/g, '');
        // 3. URL解码（兼容现代浏览器自动解码的情况）
        var decodedFileName = decodeURIComponent(encodedFileName);
        // 4. 处理特殊字符（如空格被编码为%20）
        decodedFileName = decodedFileName.replace(/%20/g, ' ');
        var blob = new Blob([response.responseBytes], { type: 'application/octet-stream' });
        var link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = decodedFileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
});