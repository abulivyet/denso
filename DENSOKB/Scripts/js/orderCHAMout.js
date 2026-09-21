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
                                { name: "长春", value: 'FTCE' },
                                { name: "一汽发动机", value: 'FTET' }
                            ],
                        },
                    },
                    ]
            },{
                border: 0,
                columnWidth: .20,
                layout: "form",
                items: [{
                    xtype: "combo",  // 使用combo代替textfield
                    id: "status",
                    fieldLabel: "解析状态",
                    store: new Ext.data.ArrayStore({
                        fields: ['value', 'text'],  // 设置存储数据的字段
                        data: [
                            ['1', '解析中'],
                            ['2', '成功'],
                            ['3', '失败'],
                            ['4', '重新解析']
                        ]
                    }),
                    displayField: 'text',  // 显示的文本
                    valueField: 'value',  // 存储的值
                    mode: 'local',  // 本地数据
                    triggerAction: 'all',  // 点击下拉框时展开所有选项
                    forceSelection: true,  // 强制选择下拉项
                    editable: false,  // 禁止手动输入
                    blankText: '请选择解析状态',  // 没有选择时的提示文本
                    emptyText: '请选择',  // 显示的默认空文本
                    anchor: '100%'  // 设置宽度填充父容器
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
                        "cuscode": Ext.getCmp("cusname").getValue(),
                        // "ordercode": Ext.getCmp("orderCode").getValue(),
                        // "orderbin": Ext.getCmp("orderBatch").getValue(),
                        "status": Ext.getCmp("status").getValue(),
                        "downloadcount": Ext.getCmp("downloadcount").getValue(),// 下载次数
                        // "createperson": Ext.getCmp("operator").getValue(),// 传票做成者 operator
                        // "importstart": Ext.getCmp("importStart").getValue(),// 导入开始时间 importStart
                        // "importend": Ext.getCmp("importEnd").getValue(),// 导入结束时间 importEnd
                        // "sendstart": Ext.getCmp("sendStart").getValue(),// 发货开始时间 sendStart
                        // "sendend": Ext.getCmp("sendEnd").getValue(),// 发货结束时间 sendEnd
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
            url: fullUrl + '/CHAMdispose/GetPageList',
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
        text: '上传文件',
        handler: function () {
            initUploadWin();

        }
    });
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
                        hidden: true,  // 初始隐藏
                        defaults: {
                            xtype: 'radiofield',
                            name: 'dataSource', // 互斥组
                            labelWidth: 120
                        },
                        items: [{
                            boxLabel: '根据【选择记录】下载',
                            inputValue: 'checked',
                            id: 'rowid',
                            hidden: true,  // 初始隐藏
                            checked: true
                        }, {
                            boxLabel: '根据【检索条件】下载',
                            inputValue: 'query',
                            hidden: true,  // 初始隐藏
                            id: 'queryid'
                        }]
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
                                    name: 'check5',
                                    inputValue: 'check5'
                                }]
                            },
                            {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '传票',
                                    name: 'check1',
                                    inputValue: 'check1'
                                }]
                            },
                            {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '托标签',
                                    name: 'check2',
                                    margin: '0 0 5 0',
                                    inputValue: 'check2'
                                },]
                            },{
                                xtype: 'checkboxfield',
                                boxLabel: '箱标签',
                                name: 'check3',
                                inputValue: 'check3',
                                margin: '0 0 5 0',
                                layout: 'column',
                                listeners: {
                                    change: function (cb, checked) {
                                      
                                    }
                                }
                            },  {
                                // 第二个复选框+单选框组
                                xtype: 'container',
                                layout: 'column',
                                margin: '-5 0 0 0',
                                items: [{
                                    xtype: 'checkboxfield',
                                    boxLabel: '取货指示单',
                                    name: 'check4',
                                    inputValue: 'check4'
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
                                && form.getValues()["check3"] == undefined && form.getValues()["check4"] == undefined
                                && form.getValues()["check5"] == undefined && form.getValues()["check7"] == undefined) {
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
                            var check3 = form.getValues()["check3"];
                            var check4 = form.getValues()["check4"];
                            var check5 = form.getValues()["check5"];
                            var check7 = form.getValues()["check7"];

                            var box = form.getValues()["box"];// 箱标签
                            console.log("下方选择项", check1 + "-1;" + check2 + "-2;" + check3 + "-3;" + check4 + "-4;" + check5 + "-5;" + check7 + "-7;");
                            var typeCheck = "";
                            if (check1 != undefined && check1 != "") { typeCheck +=  "1;"; };
                            if (check2 != undefined && check2 != "") { typeCheck += "2;"; };
                            if (check3 != undefined && check3 != "") { typeCheck +=  "3;"; };
                            if (check4 != undefined && check4 != "") { typeCheck += "4;"; };
                            if (check5 != undefined && check5 != "") { typeCheck += "5;"; };
                            if (check7 != undefined && check7 != "") { typeCheck += "7;"; };
  
                            if (value) {
                                var myArray = [];
                                for (var i = 0; i < checkedRecords.length; i++) {
                                    myArray.push({ id: checkedRecords[i].data.id, type: typeCheck, zipfilename: checkedRecords[i].data.redundancy3 });
                                }
                                console.log("传入参数", myArray);
                                onDownloadRows(myArray)
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
                        checkedRecords[i].data.handlebegin = "";
                        checkedRecords[i].data.handleend = "";
                        checkedRecords[i].data.handletime = "";
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
            enableTextSelection: true  // 关键配置  可复制文本
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
                text: '是否有补给品', dataIndex: 'hassupplies', width: 100,
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
                                    value: '', // 初始留空，后面通过请求设置
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
                                        record.set('confirmRemark', newRemark);

                                        Ext.Ajax.request({
                                            url: '/orderimport/SaveConfirmRemark',
                                            method: 'POST',
                                            jsonData: {
                                                id: record.get('id'),
                                                confirmRemark: newRemark,
                                                type: record.get('cuscode'),
                                                ordercode: record.get('ordercode')
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
                                type: record.get('cusname'),
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
                text: '传票',
                width: 60,
                xtype: 'widgetcolumn',
                onWidgetAttach: function (widget, record) {
                    //将行记录注入到图片组件
                    widget.record = record.getWidgetRecord().getData();
                },
                widget: {
                    xtype: 'image',
                    src: '../images/orderImport/book.png', // 图片路径
                    width: 20,
                    height: 20,
                    style: { cursor: 'pointer', display: 'block', margin: '0 auto' }, // 添加指针样式
                    listeners: {
                        element: 'el',
                        click: function () {

                            //showRadioWindow(this.component.getWidgetRecord().data);
                            // 直接访问注入的记录  id 类型  文件名称
                            downloadSingle(this.component.getWidgetRecord().data.id, '1');
                        }
                    }
                }
            },
            {
                text: '托标签',
                width: 60,
                xtype: 'widgetcolumn',
                onWidgetAttach: function (widget, record) {
                    //将行记录注入到图片组件
                    widget.record = record.getWidgetRecord().getData();
                },
                widget: {
                    xtype: 'image',
                    src: '../images/orderImport/label.png', // 图片路径
                    width: 20,
                    height: 20,
                    style: { cursor: 'pointer', display: 'block', margin: '0 auto' }, // 添加指针样式
                    listeners: {
                        element: 'el',
                        click: function () {
                            // 直接访问注入的记录
                            //onDownload('pallet', this.component.getWidgetRecord().data);
                            downloadSingle(this.component.getWidgetRecord().data.id, '2');
                        }
                    }
                }
            },
            {
                text: '箱标签',
                width: 60,
                xtype: 'widgetcolumn',
                onWidgetAttach: function (widget, record) {
                    //将行记录注入到图片组件
                    widget.record = record.getWidgetRecord().getData();
                },
                widget: {
                    xtype: 'image',
                    src: '../images/orderImport/box.png', // 图片路径
                    width: 20,
                    height: 20,
                    style: { cursor: 'pointer', display: 'block', margin: '0 auto' }, // 添加指针样式
                    listeners: {
                        element: 'el',
                        click: function () {
                            // 直接访问注入的记录
                            // onDownload('pallet', this.component.getWidgetRecord().data);
                            downloadSingle(this.component.getWidgetRecord().data.id, '3');
                        }
                    }
                }
            },
            {
                text: '指示单',
                width: 60,
                xtype: 'widgetcolumn',
                onWidgetAttach: function (widget, record) {
                    //将行记录注入到图片组件
                    widget.record = record.getWidgetRecord().getData();
                },
                widget: {
                    xtype: 'image',
                    src: '../images/orderImport/confirm.png', // 图片路径
                    width: 20,
                    height: 20,
                    style: { cursor: 'pointer', display: 'block', margin: '0 auto' }, // 添加指针样式
                    listeners: {
                        element: 'el',
                        click: function () {
                            // 直接访问注入的记录
                            //onDownload('pallet', this.component.getWidgetRecord().data);
                            downloadSingle(this.component.getWidgetRecord().data.id, '4');
                        }
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
        tbar: gridBotton
    });
    // 主界面布局
    Ext.create('Ext.container.Viewport', {
        layout: 'border',
        //autoScroll: true,
        items: [searchPanel, grid]
    });
    // 单个下载  ID数据ID，type 文件类型 用一个方法下载
    function downloadSingle(mid, type) {
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/CHAMdispose/DownloadFile',
            method: 'POST',
            jsonData: { type: type, id: mid},
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
    function onDownloadRows(rowdata) {  // 根据选择行下载
        var conn = new Ext.data.Connection();
        conn.request({
            url: fullUrl + '/CHAMdispose/DownloadFileZipRowid',
            method: 'POST',
            jsonData: JSON.stringify(rowdata),
            headers: {
                'Authorization': `Bearer ${token}`
            },
            binary: true, // 关键：告诉 ExtJS 处理二进制数据
            timeout: 300000,
            success: function (response) {
                DownloadResponse(response);
            },
            failure: function (response) {
                //识别是否是错误信息
                var contentType = response.getResponseHeader("Content-Type");

                // 如果是错误响应 不是zip等二进制
                if (!contentType || contentType.indexOf("application/zip") === -1) {
                    // 将 responseBytes 转成字符串（UTF-8）
                    var uint8Array = new Uint8Array(response.responseBytes);
                    var decodedText = new TextDecoder("utf-8").decode(uint8Array);

                    // 弹出错误信息
                    Ext.Msg.alert("下载失败", decodedText || "未知错误");
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