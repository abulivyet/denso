var tabPanel;
var addTab = function (e,url) {
	for (var i = 0; i < tabPanel.items.items.length; i++) {		
		if (tabPanel.items.items[i].title == e) {
			tabPanel.setActiveTab(tabPanel.items.items[i]);//将新增的tab页显示
			return;
        }			
	}
	var item = {
		title: e,
		html: '<iframe frameborder="0" src="'+url+'" style="width:100%; height:100%"></iframe>',
		closable: true //页面可关闭
	};
	tabPanel.setActiveTab(item);//将新增的tab页显示
}
Ext.onReady(function () {
	tabPanel = Ext.create('Ext.tab.Panel', {
		frame: true,
		renderTo: 'form',
		width: 500,
		height: 400,
		activeTab: 0, //默认显示的页面索引
		items: [{
			title: '首页', //tab页标题
			html: '<h2 style="color: blue;">欢迎使用</h2>' //tab页中的内容
		}],
		//buttons: [{
		//	text: '增加页面',
		//	handler: function () {
		//		var index = tabPanel.items.length + 1; //获取当前已存在的tab页数量，再加1
		//		var page = tabPanel.add({//添加一个新的tab页
		//			title: 'tab' + index,
		//			html: '<h2 style="color: blue;">this is tab' + index + '</h2>',
		//			closable: true
		//		});
		//		tabPanel.setActiveTab(page);//将新增的tab页显示
		//	}
		//}]
	});
	tabPanel.setSize(document.body.clientWidth, document.body.clientHeight);
	//设定窗体框架自适应
	Ext.EventManager.onWindowResize(function () {
		tabPanel.setSize(document.body.clientWidth, document.body.clientHeight);
	});

	
	
});