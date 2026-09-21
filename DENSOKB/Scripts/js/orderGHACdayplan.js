/*广州本田 出货指示
 * 日计划
* */

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
                columnWidth: .16, // 该列有整行中所占百分比
                layout: "form", // 从上往下的布局
                items: [
                    {
                        xtype: "combobox",
                        id: "ctype",
                        fieldLabel: "类别",
                        displayField: "name", //展示值
                        valueField: "value",
                        labelWidth: 40,
                        width: 100,
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
                                { name: "全部", value: 'ALL ' },
                                { name: "量产", value: 'PLS' },
                                { name: "八千代", value: 'EIGHT' },
                                { name: "SP", value: 'SP' },
                                { name: "阶段品", value: 'JD' }
                            ],
                        },
                    }]
            },
                {
                    border: 0,
                    columnWidth: .16,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "no",
                        fieldLabel: "取货指示号",
                    }
                    ]
                }
                , {
                    border: 0,
                    columnWidth: .16,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "plancode",
                        fieldLabel: "计划代码",
                    }
                    ]
                },

                {
                        border: 0,
                    columnWidth: .16,
                        layout: "form",
                        items: [{
                            xtype: 'datefield',
                            id: "importStart",
                            format: 'Y-m-d',
                            editable: false,
                            fieldLabel: "取货日期",
                        }]
                    }, {
                            border: 0,
                    columnWidth: .16,
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
                        "ctype": Ext.getCmp("ctype").getValue(), // 类型
                        "no": Ext.getCmp("no").getValue(), // 取货指示号
                        "plancode": Ext.getCmp("plancode").getValue(),// 计划代码
                        "importStart": Ext.getCmp("importStart").getValue(),// 开始日期
                        "importEnd": Ext.getCmp("importEnd").getValue(),// 结束日期
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
            url: fullUrl + '/GHAC/GetGHACPage_planDT',
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
                                    name: 'CONFIRM',
                                    inputValue: 'CONFIRM'
                                }]
                            }, {
                                xtype: 'checkboxfield',
                                boxLabel: '取货指示书',
                                name: 'INSTRUCTIONS',
                                inputValue: 'INSTRUCTIONS',
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
                                    boxLabel: '传票',
                                    name: 'SUMMONS',
                                    inputValue: 'SUMMONS'
                                }]
                            },{
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '托盘标签',
                                    name: 'PALLET',
                                    inputValue: 'PALLET'
                                }]
                            },{
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '箱标签',
                                    name: 'PLS',
                                    inputValue: 'PLS'
                                }]
                            },
                            {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '追溯标签',
                                    name: 'TRACE',
                                    inputValue: 'TRACE'
                                }]
                            },
                            {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '取套票',
                                    name: 'GHACoutputByCode',
                                    inputValue: 'GHACoutputByCode'
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
                            if (form.getValues()["CONFIRM"] == undefined && form.getValues()["INSTRUCTIONS"] == undefined
                                && form.getValues()["SUMMONS"] == undefined && form.getValues()["PALLET"] == undefined
                                && form.getValues()["PLS"] == undefined && form.getValues()["TRACE"] == undefined
                                && form.getValues()["GHACoutputByCode"] == undefined ) {
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
                    
                            var CONFIRM = form.getValues()["CONFIRM"];
                            var INSTRUCTIONS = form.getValues()["INSTRUCTIONS"];
                            var SUMMONS = form.getValues()["SUMMONS"];
                            var PALLET = form.getValues()["PALLET"];
                            var PLS = form.getValues()["PLS"];
                            var TRACE = form.getValues()["TRACE"];
                            var GHACoutputByCode = form.getValues()["GHACoutputByCode"];
                         
                            var typeCheck = "";
                            if (CONFIRM != undefined && CONFIRM != "") { typeCheck += CONFIRM + ","; };
                            if (INSTRUCTIONS != undefined && INSTRUCTIONS != "") { typeCheck += INSTRUCTIONS + ","; };
                            if (SUMMONS != undefined && SUMMONS != "") { typeCheck += SUMMONS + ","; };
                            if (PALLET != undefined && PALLET != "") { typeCheck += PALLET + ","; };
                            if (PLS != undefined && PLS != "") { typeCheck += PLS + ","; };
                            if (TRACE != undefined && TRACE != "") { typeCheck += TRACE + ","; };
                            if (value) {  //  根据选择记录下载
                                var myArray = [];
                                var InstructionCodeVal = "";
                                var SupplierCodeVal = "";
                                var fileType
                                //for (var i = 0; i < checkedRecords.length; i++) {
                                //    myArray.push({
                                //        InstructionCode: checkedRecords[i].data.no
                                //        , fileType: CONFIRM

                                //    });
                                //}
                                console.log("选择：", form.getValues()["INSTRUCTIONS"])
                                for (var i = 0; i < checkedRecords.length; i++) {
                                    console.log(checkedRecords[i].data.no);
                                    var ver = "";
                                    if (checkedRecords[i].data.version != null && checkedRecords[i].data.version != undefined) {
                                        ver = checkedRecords[i].data.version;
                                    }
                                    InstructionCodeVal += checkedRecords[i].data.no + ver + ",";
                                    //console.log(checkedRecords[i].data)
                                    SupplierCodeVal += checkedRecords[i].data.supplier + ","
                                }
                                console.log("数据", InstructionCodeVal)
                                if (form.getValues()["CONFIRM"] != undefined || form.getValues()["INSTRUCTIONS"] != undefined
                                    || form.getValues()["SUMMONS"] != undefined || form.getValues()["PALLET"] != undefined
                                    || form.getValues()["PLS"] != undefined || form.getValues()["TRACE"] != undefined
                                ) {
                                    onDownloadRows({
                                        InstructionCode: InstructionCodeVal,
                                        fileType: typeCheck,
                                        supplierCode: SupplierCodeVal


                                    });
                                }
                                if (form.getValues()["GHACoutputByCode"] != undefined) {
                                    if (GHACoutputByCode != undefined && GHACoutputByCode != "") {
                                        //onDownloadCT 
                                        onDownloadCT({
                                            InstructionCode: InstructionCodeVal,
                                            supplierCode: SupplierCodeVal
                                        })
                                    };
                                }
                               

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
    },);
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
                text: '上传日期', dataIndex: 'create_date', width: 120,
                filter: {
                    ype: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }

            , {
                text: '取货日期', dataIndex: 'pickdate', width: 120,
                filter: {
                    ype: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }
            , {
                text: '取货时间', dataIndex: 'picktime', width: 80,
                filter: {
                    ype: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },

            {
                text: '取货指示号', dataIndex: 'no', width: 160, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '版本', dataIndex: 'version', width: 50,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '取货车次', dataIndex: 'carno', width: 130,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '线路', dataIndex: 'line', width: 150,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '供应商', dataIndex: 'supplier', width: 80,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '供应商子工厂', dataIndex: 'suppler_sub', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '广本工厂', dataIndex: 'factory', width: 70,
                filter: { ype: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }
            , {
                text: '计划代码', dataIndex: 'plancode', width: 100,
                filter: {
                    ype: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }
            , {
                text: '物流商编码', dataIndex: 'transcode', width: 100,
                filter: {
                    ype: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '下载次数', dataIndex: 'downloadcount', width: 70,
                renderer: function (value) {
                    return value == null || value === '' ? 0 : value;
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
 
                                        // 2. 发送到后端
                                        Ext.Ajax.request({
                                            url: '/orderimport/SaveConfirmRemark',
                                            method: 'POST',
                                            jsonData: {
                                                p_code: record.get('no'),
                                                confirmRemark: newRemark,
                                                type: "广州本田"
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
                                type: '广州本田',
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
            }
            

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
        console.log("开始下载")
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/GHAC/GHACoutputSingleByCode',
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
                var contentType = response.getResponseHeader("Content-Type");

                // 如果是错误响应 不是zip等二进制
                if (!contentType || contentType.indexOf("application/zip") === -1) {
                    // 将 responseBytes 转成字符串（UTF-8）
                    var uint8Array = new Uint8Array(response.responseBytes);
                    var decodedText = new TextDecoder("utf-8").decode(uint8Array);
                    //console.log(decodedText);
                    try {
                        var obj = JSON.parse(decodedText);
                        console.log(obj.ExceptionMessage);   // 只取 ExceptionMessage
                    } catch (e) {
                        console.log("返回的不是 JSON:", decodedText);
                    }

                    // 弹出错误信息
                    Ext.Msg.alert("下载失败", obj.ExceptionMessage || "未知错误");
                    return;
                }
                //Ext.Msg.alert('错误', '文件下载失败');
            }
        });
    };
    function onDownloadCT(rowdata) {  // 根据选择行下载
        console.log("成套下载")
        console.log(rowdata)
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/GHAC/GHACoutputByCode',
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
                //Ext.Msg.alert('错误', '文件下载失败');
                var contentType = response.getResponseHeader("Content-Type");

                // 如果是错误响应 不是zip等二进制
                if (!contentType || contentType.indexOf("application/zip") === -1) {
                    // 将 responseBytes 转成字符串（UTF-8）
                    var uint8Array = new Uint8Array(response.responseBytes);
                    var decodedText = new TextDecoder("utf-8").decode(uint8Array);
                    //console.log(decodedText);
                    try {
                        var obj = JSON.parse(decodedText);
                        console.log(decodedText)
                        console.log(obj);   // 只取 ExceptionMessage
                    } catch (e) {
                        console.log("返回的不是 JSON:", decodedText);
                    }

                    // 弹出错误信息
                    Ext.Msg.alert("下载失败", decodedText);
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