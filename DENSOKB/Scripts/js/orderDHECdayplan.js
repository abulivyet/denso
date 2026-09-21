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
                columnWidth: .18,
                layout: "form",
                items: [{
                    xtype: "textfield",
                    id: "carno",
                    fieldLabel: "取货车组",
                }
                ]
            }
                , {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "p_code",
                        fieldLabel: "传票号",
                    }
                    ]
                },
                // {
                //     border: 0,
                //     columnWidth: .20,
                //     layout: "form",
                //     items: [{
                //         xtype: "combo",  // 使用combo代替textfield
                //         id: "status",
                //         fieldLabel: "解析状态",
                //         store: new Ext.data.ArrayStore({
                //             fields: ['value', 'text'],  // 设置存储数据的字段
                //             data: [
                //                 ['1', '解析中'],
                //                 ['2', '成功'],
                //                 ['3', '失败'],
                //                 ['4', '重新解析']
                //             ]
                //         }),
                //         displayField: 'text',  // 显示的文本
                //         valueField: 'value',  // 存储的值
                //         mode: 'local',  // 本地数据
                //         triggerAction: 'all',  // 点击下拉框时展开所有选项
                //         forceSelection: true,  // 强制选择下拉项
                //         editable: false,  // 禁止手动输入
                //         blankText: '请选择解析状态',  // 没有选择时的提示文本
                //         emptyText: '请选择',  // 显示的默认空文本
                //         anchor: '100%'  // 设置宽度填充父容器
                //     }]
                // },
                {
                    border: 0,
                    columnWidth: .20,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "downloadcount",
                        fieldLabel: "下载次数",
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
                        "carno": Ext.getCmp("carno").getValue(),
                        // "plancode": Ext.getCmp("plancode").getValue(),
                        "pn": Ext.getCmp("p_code").getValue(),
                        // "status": Ext.getCmp("status").getValue(),
                        "downloadcount": Ext.getCmp("downloadcount").getValue(),// 下载次数

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
            url: fullUrl + '/DHECdispose/GetPageDayPlanList',
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
        text: '标签下载',
        handler: function () {
            var form = Ext.create('Ext.form.Panel', {
                border: false,
                width: '100%',
                bodyPadding: 10,
                items: [
                    {
                        // 数据对象部分
                        xtype: 'fieldset',
                        title: '数据对象',

                        defaults: {
                            xtype: 'radiofield',
                            name: 'dataSource', // 互斥组
                            labelWidth: 120
                        },
                        items: [{
                            boxLabel: '根据【选择记录】下载',
                            inputValue: 'checked',
                            id: 'rowid',
                            hidden: true,
                            checked: true
                        },
                            {
                            boxLabel: '根据【检索条件】下载',
                                inputValue: 'query',
                                hidden:true,
                            id: 'queryid'
                            }
                        ]
                    },
                    {
                        // 第一个复选框+单选框组
                        xtype: 'fieldset',
                        title: '标签类型',
                        items: [
                            {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '确认表',
                                    name: 'check1',
                                    inputValue: 'check1'
                                }]
                            }, {
                                xtype: 'checkboxfield',
                                boxLabel: '箱标签',
                                name: 'check2',
                                inputValue: 'check2',
                                margin: '0 0 5 0',
                                layout: 'column',
                                listeners: {
                                    change: function (cb, checked) {
                                        //获取关联的单选框组并切换可见性
                                        var radioGroup = cb.up().down('radiogroup');

                                    }
                                }
                            }, 
                            {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '取套票',
                                    name: 'check7',
                                    inputValue: 'check7'
                                }]
                            }]
                    }]

            });

            var win = Ext.create('Ext.window.Window', {
                title: '标签下载',
                modal: true,
                width: 500,
                height: 340,
                items: form,
                constrain: true,
                autoShow: true,
                buttons: [{
                    text: '确定',
                    handler: function () {
                        if (form.isValid()) {
                            //如果没有选择数据对象提示
                            if (form.getValues()["check1"] == undefined && form.getValues()["check2"] == undefined                                
                                && form.getValues()["check7"] == undefined ) {
                                parent.Ext.Msg.alert('提示', '请选择标签类型');
                                return;
                            };
                            //选择的数据
                            var checkedRecords = grid.getSelectionModel().getSelection();
                            console.log("选择的数据", checkedRecords)
                            //如果先择了选择记录下载，判断选择记录是否为空
                            if (form.getValues()["dataSource"] == "checked" && checkedRecords.length == 0) {
                                parent.Ext.Msg.alert('提示', '请勾选明细行数据');
                                return;
                            }
                            var records = grid.getStore().getRange(0, store.getTotalCount());
                            console.log("检索条件", records)

                            var radioButton = Ext.getCmp('rowid');  //  根据选择记录下载
                            var radioqueryid = Ext.getCmp('queryid') // 根据检索条件下载
                            var value = radioButton.getValue();  // 获取选中状态
                            var jsvalue = radioqueryid.getValue();
                            console.log("选择", "选择下载：" + value + ";检索下载：" + jsvalue);
                            var check1 = form.getValues()["check1"];
                            var check2 = form.getValues()["check2"];
                            var check7 = form.getValues()["check7"];
                            console.log("下方选择项", check1 + "-1;" + check2 + "-2;" + check7 + "-7;");
                            var typeCheck = "";
                            if (check1 != undefined && check1 != "") { typeCheck += check1 + ";"; };
                            if (check2 != undefined && check2 != "") { typeCheck += check2 + ";"; };
                            if (check7 != undefined && check7 != "") { typeCheck += check7 + ";"; };
                            if (value) {  //  根据选择记录下载
                                var myArray = [];
                                for (var i = 0; i < checkedRecords.length; i++) {
                                    myArray.push({
                                          carno: checkedRecords[i].data.carno
                                        , plancode: checkedRecords[i].data.plancode
                                        , pn: checkedRecords[i].data.pn
                                        , plandate: checkedRecords[i].data.plandate
                                        , plantime: checkedRecords[i].data.plantime
                                        , "check1": check1 ||null
                                        , "check2": check2 || null
                                        , "check7": check7 || null
                                    });
                                }
                                console.log("传入参数", myArray);
                                onDownloadRows(myArray)
                            }
                            if (jsvalue) {

                                var queryJson = {
                                    "cuscode": Ext.getCmp("cusname").getValue(),
                                    "ordercode": Ext.getCmp("orderCode").getValue(),
                                    "orderbin": Ext.getCmp("orderBatch").getValue(),

                                };

                                //  myArray.push({ query: checkedRecords[i].data.id, type: typeCheck, zipfilename: checkedRecords[i].data.redundancy3 });

                                onDownloadJS({
                                    query: JSON.stringify(queryJson),
                                    type: typeCheck
                                })
                            }
                            win.close();
                        }

                    }
                }]
            });
            $('.x-mask').css('background-color', '#000');
            $('.x-mask').css('opacity', '0.2');
            win.show();
        }
    });
    gridBotton.push({
        text: '填写备注',
        handler: function () {
            var form = Ext.create('Ext.form.Panel', {
                border: false,
                width: '100%',
                bodyPadding: 10,
                items: [                  
                    {
                        // 第一个复选框+单选框组
                        xtype: 'textareafield',
                        title: '备注',
                        name: 'remark',
                        width: '100%',
                        height:'250px'
                     
                    }]

            });

            var win = Ext.create('Ext.window.Window', {
                title: '填写备注',
                width: 500,
                height: 340,
                items: form,
                constrain: true,
                modal: false,
                autoShow: true,
                buttons: [{
                    text: '确定',
                    handler: function () {
                        if (form.isValid()) {
                            //选择的数据
                            var checkedRecords = grid.getSelectionModel().getSelection();
                            if (checkedRecords == null || checkedRecords == undefined|| checkedRecords.length == 0) {
                                parent.Ext.Msg.alert('提示', '请勾选明细行数据');
                                return;
                            }
                          
                            win.close();
                        }
                    }
                }]
            });
            $('.x-mask').css('background-color', '#000');
            $('.x-mask').css('opacity', '0.2');
            win.show();
        }
    });
    Ext.tip.QuickTipManager.init(); // 初始化提示管理器
    // 创建Grid面板
    var grid = Ext.create('Ext.grid.Panel', {
        //renderTo: Ext.getBody(),
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
            enableTextSelection: true  // 关键配置
        },
        columns: [

            {
                text: '取货车组', dataIndex: 'carno', width: 140, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '取货传票号', dataIndex: 'p_code', width: 250,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '确认表备注',
                width: 100,
                xtype: 'widgetcolumn',
                onWidgetAttach: function (widget, record) {
                    widget.record = record;
                },
                widget: {
                    xtype: 'button',
                    text: '填写',
                    width: 60,
                    handler: function () {
                        var record = this.getWidgetRecord();
                        //var existingRemark = record.get('confirmRemark') || '';

                        var win = Ext.create('Ext.window.Window', {
                            title: '填写确认表备注',
                            modal: true,
                            width: 600,
                            height: 400,
                            layout: 'fit',
                            items: [{
                                xtype: 'form',
                                bodyPadding: 10,
                                items: [{
                                    xtype: 'textarea',
                                    name: 'confirmRemark',
                                    fieldLabel: '备注',
                                    labelAlign: 'top',
                                    value: '',
                                    allowBlank: false,
                                    anchor: '100%',
                                    height: 300
                                }]
                            }],
                            buttons: [{
                                text: '确定',
                                handler: function () {
                                    var form = win.down('form').getForm();

                                    if (form.isValid()) {
                                        var newRemark = form.findField('confirmRemark').getValue();

                                        // 1. 更新 Grid 数据
                                        record.set('confirmRemark', newRemark);
                                        console.log(record);
                                        console.log(record.get('carno'));
                                        console.log(record.get('p_code'));
                                        // 2. 发送到后端
                                        Ext.Ajax.request({
                                            url: '/orderimport/SaveConfirmRemark',
                                            method: 'POST',
                                            jsonData: {
                                                carno: record.get('carno'),
                                                p_code: record.get('p_code'),
                                                confirmRemark: newRemark,
                                                type: "东风本田"
                                            },
                                            success: function () {
                                                Ext.Msg.alert('成功', '备注已保存');
                                                win.close();
                                            },
                                            failure: function () {
                                                Ext.Msg.alert('错误', '备注保存失败');
                                            }
                                        });
                                    }
                                }
                            }, {
                                text: '取消',
                                handler: function () {
                                    win.close();
                                }
                            }]
                        });

                        win.show();

                        // 发请求加载确认表备注
                        Ext.Ajax.request({
                            url: '/orderimport/GetConfirmRemark',
                            method: 'GET',
                            params: {
                                id: record.get('id'),
                                type: "东风本田",
                                ordercode: record.get('ordercode')
                            },
                            success: function (response) {
                                var resp = Ext.decode(response.responseText);
                                var remark = resp.info || '';  // 从 info 读取备注
                                var form = win.down('form').getForm();
                                form.findField('confirmRemark').setValue(remark);
                            },

                            failure: function () {
                                Ext.Msg.alert('错误', '无法加载备注信息');
                            }
                        });
                    }
                }
            },
            {
                text: '下载次数', dataIndex: 'downloadcount', width: 70,
                renderer: function (value) {
                    return value == null || value === '' ? 0 : value;
                }
            },
            {
                text: '计划离开日期', dataIndex: 'planleavedate', width: 140,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '计划离开时间', dataIndex: 'planleavetime', width: 120,
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
    function onDownloadRows(rowdata) {  // 根据选择行下载
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/DHECdispose/download',
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
                //console.log("response错误",response)
                //Ext.Msg.alert('错误', '文件下载失败');
                //识别是否是错误信息
                var contentType = response.getResponseHeader("Content-Type");

                
                if (!contentType || !contentType.includes("application/zip")) {
                    // 1. 将 responseBytes 转成字符串
                    var uint8Array = new Uint8Array(response.responseBytes);
                    var decodedText = new TextDecoder("utf-8").decode(uint8Array);

                    // 解析 JSON 并提取 message
                    try {
                        var json = JSON.parse(decodedText);
                        Ext.Msg.alert("下载失败", json.message || "未知错误");
                    } catch (e) {
                        // 解析失败就直接显示原始字符串
                        Ext.Msg.alert("下载失败", decodedText || "未知错误");
                    }
                    return;
                }
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