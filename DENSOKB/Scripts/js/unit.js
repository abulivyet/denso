var fullUrl = window.location.origin;
var initUploadWin;
var type;
var downloadFile;
var downloadType;
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
                    id: "cust_name",
                    fieldLabel: "客户简称",
                }
                ]
            },
                {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "cust_code",
                        fieldLabel: "客户代码",
                    }
                    ]
                }
                 ,{
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "recv_port",
                        fieldLabel: "受入口",
                    }
                    ]
                }, {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "veh_model",
                        fieldLabel: "车型",
                    }
                    ]
                }, {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "back_num",
                        fieldLabel: "背番",
                    }
                    ]
                },{
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "denso_num",
                        fieldLabel: "电装品番",
                    }
                    ]
                }, {
                    border: 0,
                    columnWidth: .18,
                    layout: "form",
                    items: [{
                        xtype: "textfield",
                        id: "cust_num",
                        fieldLabel: "客户品番",
                    }
                    ]
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
                        "cust_name": Ext.getCmp("cust_name").getValue(),    //客户简称
                        "cust_code": Ext.getCmp("cust_code").getValue(),    //客户代码
                        "recv_port": Ext.getCmp("recv_port").getValue(),    //受入口
                        "veh_model": Ext.getCmp("veh_model").getValue(),    //车型
                        "back_num": Ext.getCmp("back_num").getValue(),    //背番
                        "denso_num": Ext.getCmp("denso_num").getValue(),    //电装品番
                        "cust_num": Ext.getCmp("cust_num").getValue(),    //客户品番


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
            { name:'ordercode'},
            { name: 'labeldate', type: 'date', dateFormat: 'Y-m-d'  },
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
        //fields: ['inportdate', 'cusname', 'cuscode', 'ordercode', 'orderbin',
        //    'podate', 'createperson', 'createdate'],
        proxy: {
            type: 'ajax',
            url: fullUrl + '/chain/getUnitPage',
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
        text: '丰田导入',
        handler: function () {
            type = 'toyota';
            initUploadWin();

        }
    });
    gridBotton.push({
        text: '本田导入',
        handler: function () {
            type = 'honda';
            initUploadWin();

        }
    });
    gridBotton.push({
        text: '丰田导出',
        handler: function () {
            downloadFile('toyota');

        }
    });
    gridBotton.push({
        text: '本田导出',
        handler: function () {
            downloadFile('honda');

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
        columns: [

            {
                text: '客户简称', dataIndex: 'cust_name', width: 90, editor: 'textfield',
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '客户代码', dataIndex: 'cust_code', width: 90,
                
            },
            {
                text: '受入口', dataIndex: 'recv_port', width: 60,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '车型', dataIndex: 'veh_model', width: 130,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '背番', dataIndex: 'back_num', width: 60,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '受入区代码', dataIndex: 'recv_area', width: 80,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            }, {
                text: '交货地点', dataIndex: 'del_loc', width: 80,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },

            {
                text: '电装品番', dataIndex: 'denso_num', width: 140,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '客户品番', dataIndex: 'cust_num',  width: 140,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '客户品番', dataIndex: 'custNum', width: 130,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '品番名称', dataIndex: 'part_name', width: 140,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '收容数', dataIndex: 'box_cap', width: 60,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '拖标箱数', dataIndex: 'pallet_box', width: 60,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '箱型号', dataIndex: 'box_type', width: 90,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '库位号', dataIndex: 'loc_num', width: 70,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '拖占比', dataIndex: 'pallet_pct', width: 60,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '组合打托', dataIndex: 'comb_pallet', width: 70,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '追溯标签', dataIndex: 'traceability_label', width: 70,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '品名中文', dataIndex: 'product_name_cn', width: 100,
                filter: {
                    type: 'string', // 字符串类型筛选
                    placeholder: '输入...'
                }
            },
            {
                text: '厂别', dataIndex: 'factory', width: 90,
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

    var filesStore = Ext.create('Ext.data.Store', {
        id:"filesStore",
        fields: ['name', 'size', 'type', 'file'],
        data: []
    });
    function addFilesToStore(files) {
        var fileData = [];
        for (var i = 0; i < files.length; i++) {
            fileData.push({
                name: files[i].name,
                size: files[i].size,
                type: files[i].type,
                file: files[i]
            });
        }
        filesStore.add(fileData);
    }

    function uploadFiles(files,btn,type) {
        var formData = new FormData();
        for (var i = 0; i < files.length; i++) {
            formData.append('files', files[i].file); // 将文件添加到 FormData
        }
        formData.append("typeCode", type);
        console.log(formData);
        // 使用 Ext.Ajax 上传文件
        Ext.Ajax.request({
            url: '/Unit/UploadFile', // 服务器端上传地址
            method: 'POST',
            rawData: formData,
            headers: {
                'Content-Type': null
            },
            disableJson:true,
            processData: false, // 不处理数据
            success: function (response) {
                if (btn) btn.enable();
                var result = Ext.JSON.decode(response.responseText);
                if (result.success) {

                    filesStore.removeAll();
                    uploadFileswin.close();

                    // 修改为使用 message 字段
                    Ext.Msg.alert('成功', result.message);

                    // chy  成功之后刷新一下数据
                    var btn = Ext.ComponentQuery.query('#searchBtn')[0];
                    btn.fireEvent('click');
                    Ext.getCmp('tableGrid').getStore().load();

                } else {
                    Ext.Msg.alert('失败', result.message);
                }

            },
            failure: function (response) {
                if (btn) btn.enable();
                Ext.Msg.alert('错误', '文件上传失败');
            }
        });
    }

    downloadFile = function (downloadType) {
        if (!downloadType) {
            Ext.Msg.alert('错误', '类别不能为空');
            return;
        }
        var url = '/Unit/DownloadFile?type=' + encodeURIComponent(downloadType);
        var a = document.createElement('a');
        a.href = url;
        // a.download = filename; // 可选，浏览器使用的保存名
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }


    initUploadWin = function() {
        uploadFileswin = new Ext.window.Window({
            id: 'openwin',
            width: 500,
            height: 380,
            title: '导入文件',
            constrain: true,
            //autoShow: true,
            modal: true,
            resizable: false,
            items: [
                Ext.create('Ext.grid.Panel', {
                    id: "grid_filelist",
                    width: 500,
                    height: 250,
                    store: filesStore,
                    columns: [
                        { text: '文件名', dataIndex: 'name', flex: 1 },
                        { text: '大小 (字节)', dataIndex: 'size', width: 100 },
                        { text: '类型', dataIndex: 'type', width: 100 }
                    ]
                }),
                {
                    border: false,
                    html: '<div class="drop-zone" style="width:500px;height:55px;  background-color:#f0f0f0; text-align:center;line-height:50px;border-bottom:1px solid #c0c0c0;cursor: pointer;">选择文件或将文件拖拽到此区域</div>',
                    listeners: {
                        afterrender: function (cmp) {
                            var dropZone = cmp.getEl().down('.drop-zone').dom;

                            // 阻止默认行为
                            function preventDefault(e) {
                                e.preventDefault();
                                e.stopPropagation();
                            }

                            // 高亮拖拽区域
                            function highlight() {
                                dropZone.style.backgroundColor = '#ffc';
                            }

                            // 取消高亮拖拽区域
                            function unhighlight() {
                                dropZone.style.backgroundColor = '#f0f0f0';
                            }

                            // 监听拖拽事件
                            dropZone.addEventListener('dragenter', preventDefault, false);
                            dropZone.addEventListener('dragover', function (e) {
                                preventDefault(e);
                                highlight();
                            }, false);
                            dropZone.addEventListener('dragleave', function (e) {
                                preventDefault(e);
                                unhighlight();
                            }, false);
                            dropZone.addEventListener('drop', function (e) {
                                preventDefault(e);
                                unhighlight();

                                // 获取拖拽的文件
                                var files = e.dataTransfer.files;
                                if (files.length > 0) {
                                    addFilesToStore(files);
                                }
                            }, false);
                            dropZone.addEventListener('click', function (e) {
                                // $('#filesbtn .x-form-file-input')[0].click()
                                var fileFieldCmp = Ext.getCmp('filesbtn');
                                if (fileFieldCmp && fileFieldCmp.fileInputEl) {
                                    fileFieldCmp.fileInputEl.dom.click();
                                } else {
                                    console.warn('找不到 filefield 的 input 元素');
                                }
                            }, false);
                        }
                    }
                },
                {
                    style: 'display:none',
                    xtype: 'filefield',
                    id: 'filesbtn',
                    fieldLabel: '选择文件',
                    allowBlank: false,
                    buttonText: '浏览...',
                    listeners: {
                        afterrender: function (cmp) {
                            // 设置 input 元素的 multiple 属性
                            cmp.fileInputEl.set({ multiple: 'multiple' });
                        },
                        change: function (field, value) {
                            // 获取文件对象（通过 DOM 元素的 files 属性）
                            var fileInput = field.fileInputEl.dom;
                            addFilesToStore(fileInput.files);

                        }
                    }
                }
            ],
            buttons: [
                Ext.create('Ext.Button', {
                    text: '上传文件',
                    id:"uploadbutton",
                    handler: function () {
                        var files = [];
                        filesStore.each(function (record) {
                            files.push(record.data);
                        });

                        if (files.length > 0) {
                            var btn = this;
                            // 禁用按钮
                            btn.disable();
                            
                            uploadFiles(files, btn,type);
                        } else {
                            Ext.Msg.alert('提示', '请先拖拽文件到上传区域');
                        }
                    }
                }),
                Ext.create('Ext.Button', {
                    text: '清空选择',
                    handler: function () {
                        clearUploadFiles();
                        var btn = Ext.getCmp('uploadbutton');// 获取上传附件按钮
                        if (btn) btn.enable();
                    }
                })
            ]
        });

        uploadFileswin.show();
    }
    
    // 主界面布局
    Ext.create('Ext.container.Viewport', {
        layout: 'border',
        //autoScroll: true,
        items: [searchPanel, grid]
    });

});