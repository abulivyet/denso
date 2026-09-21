var uploadFileswin;
var initUploadWin;
Ext.onReady(function () {

    var filesStore = Ext.create('Ext.data.Store', {
        id:"filesStore",
        fields: ['name', 'size', 'type', 'file'],
        data: []
    });

    // 添加文件到 Store
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

    // 文件上传函数
    function uploadFiles(files,btn) {
        var formData = new FormData();
        for (var i = 0; i < files.length; i++) {
            formData.append('files', files[i].file); // 将文件添加到 FormData
        }
        formData.append("username", localStorage.user);
        console.log(formData);
        // 使用 Ext.Ajax 上传文件
        Ext.Ajax.request({
            url: '/orderImport/UploadFile', // 服务器端上传地址
            method: 'POST',
            rawData: formData,
            headers: {
                'Content-Type': null
            },
            timeout: 9000000,
            disableJson:true,
            processData: false, // 不处理数据
            success: function (response) {
                if (btn) btn.enable();
                var result = Ext.JSON.decode(response.responseText);
                if (result.success) {
                    
                    //Ext.getCmp('grid').getStore().removeAll(); // 清空Store
                    //Ext.getCmp('grid').getStore().load();

                    filesStore.removeAll();
                    uploadFileswin.close();
                    Ext.Msg.alert('成功', result.result);
                    // chy  成功之后刷新一下数据
                    var btn = Ext.ComponentQuery.query('#searchBtn')[0]; 
                    btn.fireEvent('click');
                    Ext.getCmp('tableGrid').getStore().load();
                } else {
                    Ext.Msg.alert('失败', result.result);
                }
            },
            failure: function (response) {
                if (btn) btn.enable();
                Ext.Msg.alert('错误', '文件上传失败');
            }
        });
    }
    function check(files,btn) {
        var formData = new FormData();
        for (var i = 0; i < files.length; i++) {
            formData.append('files', files[i].file); // 将文件添加到 FormData
        }
        Ext.Ajax.request({
            url: '/orderImport/fileNameCheck', // 服务器端上传地址
            method: 'POST',
            rawData: formData,
            headers: {
                'Content-Type': null
            },
            disableJson: true,
            timeout: 300000,
            processData: false, // 不处理数据
            success: function (response) {
                console.log("验证文件",response)
                var result = Ext.JSON.decode(response.responseText);
                if (result.success) {
                    uploadFiles(files, btn);
                } else {
                   Ext.Msg.alert('操作失败', result.result);
                    //Ext.Msg.confirm('是否继续', result.result, function (btnok) {
                    //    if (btnok === 'yes') {
                    //        // 用户点击了"是"
                    //        uploadFiles(files, btn);
                    //    } else {
                           
                    //        if (btn) btn.enable();
                          
                    //    }
                    //});
                }
            },
            failure: function (response) {
                if (btn) btn.enable();
                Ext.Msg.alert('错误', '文件上传失败');
            }
        });
    }
    function clearUploadFiles() {
        // 清空文件列表的 Store
        var filesStore = Ext.getStore('filesStore'); // 或者直接使用你已有的 filesStore 引用
        filesStore.removeAll();
        var fileField = Ext.getCmp('filesbtn'); // 根据你的实际组件ID调整
        if (fileField) {
            fileField.reset();
        }
        Ext.getCmp('grid_filelist').getView().refresh();
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
                            // 校验是否有重复文件
                            check(files,btn);

                            //uploadFiles(files, btn);
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
    
    


});