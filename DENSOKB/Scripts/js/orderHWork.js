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
                        xtype: "combobox",
                        id: "cusname",
                        fieldLabel: "客户",
                        displayField: "name", //展示值
                        valueField: "value",
                        labelWidth: 60,
                        width: 140,
                        fuzzySelect: true,
                        queryMode: "local", //取本地数据
                        multiSelect: false, //支持多选
                        hidden: false, //是否隐藏,
                        editable: false,
                        allowBlank: true, //允许value为空
                        msgTarget: "qtip",
                        store: {
                            fields: ["name", "value"],
                            data: [
                                { name: "佛山本田", value: 'CHAM' },
                                { name: "东风本田", value: 'DHEC' },
                                { name: "广州本田", value: 'GHAC' }
                            ],
                        },
                    }]

            },
                
            {
                border: 0,
                columnWidth: .18,
                layout: "form",
                items: [{
                    xtype: "textfield",
                    id: "otype",
                    fieldLabel: "类型",
                }
                ]
            }, {
                border: 0,
                columnWidth: .20,
                layout: "form",
                items: [{
                    xtype: "textfield",
                    id: "operator",
                    fieldLabel: "传票作成者",
                }]
            }]
        }, { // 行2
            layout: "column",
            border: 0,
            items: [{
                border: 0,
                columnWidth: .19,
                layout: "form",
                items: [{
                    xtype: 'datefield',
                    id: "importStart",
                    format: 'Y-m-d',
                    editable: false,
                    fieldLabel: "导入时间",
                }]
            }, {
                border: 0,
                columnWidth: .19,
                layout: "form",
                items: [{
                    xtype: 'datefield',
                    id: "importEnd",
                    format: 'Y-m-d',
                    editable: false,
                    labelSeparator: '',
                    fieldLabel: "至",
                }]
            }, 
               
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
                        "cuscode": Ext.getCmp("cusname").getValue(),   // 客户
                        "otype": Ext.getCmp("otype").getValue(),         // 类型
                        "createperson": Ext.getCmp("operator").getValue(),// 传票做成者 operator
                        "importstart": Ext.getCmp("importStart").getValue(),// 导入开始时间 importStart
                        "importend": Ext.getCmp("importEnd").getValue(),// 导入结束时间 importEnd
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
            url: fullUrl + '/orderHWork/GetPageList',
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
            //extractResponseData: function (response) {
            //    console.log("response-------", response.status);
            //}
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
            enableTextSelection: true  // 关键配置
        },
        columns: [
            {
                text: '导入时间',
                dataIndex: 'inportdate',
                width: 150,
                renderer: Ext.util.Format.dateRenderer('Y-m-d H:i:s'),
                filter: {
                    type: 'date', // 日期类型筛选
                    dateFormat: 'Y-m-d H:i:s'
                },

            },
            {
                text: '解析状态', dataIndex: 'handlestate', width: 80,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                },
                renderer: function (value) {
                    const map = {
                        '1': '解析中',
                        '2': '成功',
                        '3': '失败',
                        '4': '重新解析'
                    };
                    return map[value] || value;
                }
            },
            {
                text: '解析结果', dataIndex: 'handlemess', width: 250,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                },
                renderer: function (v, meta) {
                    meta.tdAttr = 'data-qwidth=300 data-qtip="' + v + '"';
                    return v;
                }
            },
            {
                text: '客户', dataIndex: 'cusname', width: 120, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '客户代码', dataIndex: 'cuscode', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '订单号', dataIndex: 'ordercode', width: 150,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '类型', dataIndex: 'redundancy2', width: 70,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '订单便次', dataIndex: 'orderbin', width: 80,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '发货日期',
                dataIndex: 'podate',
                width: 100,
                renderer: Ext.util.Format.dateRenderer('Y-m-d'),
                filter: {
                    type: 'date', // 日期类型筛选
                    dateFormat: 'Y-m-d H:i:s'
                }
            },
            {
                text: '传票作成者', dataIndex: 'createperson', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '传票作成时刻',
                dataIndex: 'createdate',
                width: 150,
                renderer: Ext.util.Format.dateRenderer('Y-m-d H:i:s'),
                filter: {
                    type: 'date', // 日期类型筛选
                    dateFormat: 'Y-m-d H:i:s'
                }
            },
            {
                text: '文件名称', dataIndex: 'redundancy3', width: 180,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },

            {
                text: '解析开始时间', dataIndex: 'handlebegin', width: 180,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '解析结束时间', dataIndex: 'handleend', width: 180,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '时长', dataIndex: 'handletime', width: 180,
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
        tbar: [{
            text: '上传文件',
            handler: function () {
                initUploadWin();

            }
        },
        {
            text: '重新解析',
            handler: function () {
                Ext.Msg.alert('确认操作', '是否重新解析此文件', function (btnId) {
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

                            // 重新解析只能 2 ，3 重新解析
                            //if (checkedRecords[i].data.handlestate == '3' || checkedRecords[i].data.handlestate == '2') {
                            checkedRecords[i].data.handlestate = '4';
                            checkedRecords[i].data.handlemess = "重新解析中"
                            myArray.push(checkedRecords[i].data);
                            //} else {
                            //    parent.Ext.Msg.alert('提示', '数据'+checkedRecords[i].data.inportdate+'_'+checkedRecords[i].data.redundancy3+'无法重新解析');
                            //    return;
                            //}

                        }
                        batchPost('/orderImport/order_Re_analyze', myArray);
                    }
                });

            }
            },
            {
                text: '删除数据',
                handler: function () {
                    var checkedRecords = grid.getSelectionModel().getSelection();
                    if (checkedRecords.length == 0) {
                        parent.Ext.Msg.alert('提示', '请选择行数据');
                        return;
                    } else {
                        Ext.Msg.alert('确认操作', '是否确认删除【' + checkedRecords.length + '】行数据', function (btnId) {
                            if (btnId === 'ok') {
                                var myArray = [];
                                for (var i = 0; i < checkedRecords.length; i++) {
                                    myArray.push({
                                        id: checkedRecords[i].data.id,
                                    })
                                }
                                Ext.Ajax.request({
                                    url: '/orderImport/delimport', // 服务器端上传地址
                                    method: 'POST',
                                    jsonData: myArray,
                                    headers: {
                                        'Authorization': `Bearer ${token}`,
                                        'Content-Type': 'application/json'

                                    },
                                    success: function (response) {
                                        console.log("验证文件", response)
                                        var result = Ext.JSON.decode(response.responseText);
                                        Ext.Msg.alert("提示", result.result);
                                        Ext.getCmp('tableGrid').getStore().load();
                                    },

                                    failure: function (response) {
                                        Ext.Msg.alert('错误', response);
                                        Ext.getCmp('tableGrid').getStore().load();
                                    }
                                });
                            }
                        })
                    }
                }
            }
        ]
    });
    // 主界面布局
    Ext.create('Ext.container.Viewport', {
        layout: 'border',
        //autoScroll: true,
        items: [searchPanel, grid]
    });
    //点击受领书显示单选
    function showRadioWindow(record) {
        console.log(record);
        var win = Ext.create('Ext.window.Window', {
            title: '标签类型',
            layout: 'column',
            width: 250,
            height: 150,
            buttonAlign: 'center',
            layout: {
                type: 'vbox',        // 使用 vbox 布局
                align: 'center',      // 垂直方向居中
                pack: 'center'        // 水平方向居中
            },
            items: [{
                xtype: 'radiogroup',
                columns: 1,
                items: [
                    { boxLabel: '小纸张', name: 'type', inputValue: 'boxMin', checked: true },
                    { boxLabel: 'A4穿透', name: 'type', inputValue: 'boxA4' }
                ]
            }],
            buttons: [{
                text: '确定',
                handler: function () {
                    var radioGroup = win.down('radiogroup');
                    var selected = radioGroup.getChecked()[0];
                    if (selected) {
                        downloadFile2(record.id, selected.inputValue, selected.boxLabel + "");
                    }
                    win.close();
                }
            }, {
                text: '取消',
                handler: function () { win.close(); }
            }]
        });
        win.show();
    };
    //适用于 GET 请求
    function downloadFile1(type) {
        // 构建 URL 参数
        var params = {
            type: type
        };
        var query = Object.keys(params)
            .map(key => encodeURIComponent(key) + '=' + encodeURIComponent(params[key]))
            .join('&');

        // 创建临时链接并触发点击
        var url = fullUrl + '/DownloadFile/DownloadFile?' + query; // 替换为你的接口地址
        var link = document.createElement('a');
        link.href = url;
        link.download = 'filename.ext'; // 可选，但需后台支持文件名
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };
    //适用于 POST 请求
    function downloadFile2(mid, type, filename) {
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/DownloadFile/DownloadFileZipNew',
            method: 'POST',
            jsonData: { type: type, id: mid, zipfilename: filename },
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                console.log("responsesuccess", response.message)
                DownloadResponse(response);
                //if (response.statusText == "ok") {
                //    DownloadResponse(response);
                //} else {
                //    Ext.Msg.alert('错误', decodeURIComponent(response.message));
                //}
            },
            failure: function (response) {
                console.log("responsefailure", response)
                Ext.Msg.alert('错误', "下载失败");
            }
        });
    }
    function onDownloadRows(rowdata) {  // 根据选择行下载
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/DownloadFile/DownloadFileZipRowid',
            method: 'POST',
            jsonData: rowdata,
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                DownloadResponse(response);
            },
            failure: function (response) {
                Ext.Msg.alert('错误', '文件下载失败');
            }
        });
    }
    function onDownloadJS(rowdata) {  // 根据选择检索条件下载
        console.log("根据检索条件下载", rowdata)
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/DownloadFile/DownloadFileZipJS',
            method: 'POST',
            jsonData: rowdata,
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                DownloadResponse(response);
            },
            failure: function (response) {
                Ext.Msg.alert('错误', '文件下载失败');
            }
        });
    }
    // 尝试下载只用一个方法，下载方法写的有点多
    function batchPost(url, rowdata) {
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + url,
            method: 'POST',
            jsonData: rowdata,
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                console.log(response);
            },
            failure: function (response) {
                Ext.Msg.alert('错误', '文件下载失败');
            }
        });
    }
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