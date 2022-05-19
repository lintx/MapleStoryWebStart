## MapleStoryWebStart(新枫之谷网页启动器)

### 说明
游戏橘子香港站于2022年5月更新，旧的登录网页将于2022年6月8日失效，因为本人是简体环境，启动游戏依赖[Beanfun](https://github.com/pungin/Beanfun)，启动方式更新后，`Beanfun`也需要做对应的更新才能使用。
 
研究后本人认为新的网页启动方式相较于旧的启动方式只能使用IE来说，简单很多，而登录器整合的方式暂时无法解决谷歌验证的问题。

所以制作了新版网页启动的中间程序，使用简单，只需要简单的点一两下按钮，之后直接使用网页启动即可在非繁体环境下启动游戏。

>注：网页启动会自动登录到选区界面，如果需要登录到账号登录界面的可以直接运行中间程序，但是新版网页我暂时没有找到获取账号密码的地方，所以这个功能可能并没有什么用。

### 下载
<https://github.com/LinTx/MapleStoryWebStart/releases/latest>

### 使用
1. 国内推荐使用edge浏览器，以便使用edge应用商店，如果你可以使用谷歌应用商店，则随意使用。
2. 使用edge、Chrome等浏览器打开[游戏橘子新版官网](https://bfweb.hk.beanfun.com/)后登录游戏帐号。
3. 如果因为无法显示谷歌验证码而无法登录，可以尝试使用[Gooreplacer插件](https://microsoftedge.microsoft.com/addons/detail/gooreplacer/cidbonnpjopamnhfjdgfcmjmlmehjnej?hl=zh-CN)。
4. 按照[游戏橘子元件安装引导](https://bfweb.hk.beanfun.com/bfevent/bf/webstart/index.html)安装对应的浏览器插件和beanfun！游戏Plugin。
5. 谷歌Chrome应用商店被墙的玩家可以使用edge浏览器，并至[edge应用商店下载浏览器插件](https://microsoftedge.microsoft.com/addons/detail/%E9%81%8A%E6%88%B2%E6%A9%98%E5%AD%90%E6%93%B4%E5%85%85%E5%85%83%E4%BB%B6/jglicoinfpfkfcoeahbcofiplegbflhh?hl=zh-CN)。
6. 下载本程序并解压（注：不可放到游戏文件夹下，因为本程序有一个文件的名字必须是`MapleStory.exe`，放到游戏文件夹下会冲突）。
7. 打开`Setting.exe`，点击`选择`按钮后，找到游戏主文件后确定，左侧输入框会显示游戏目录。
8. 点击`设置`按钮，如提示设置成功，即完成了本程序的设置。
9. 打开[游戏橘子游戏库](https://bfweb.hk.beanfun.com/game_zone/)找到新枫之谷后，点击启动即可进入游戏。

### 卸载
打开`Setting.exe`，点击`删除`按钮并得到正确提示后即卸载成功。

### 原理
> 在网页点击启动游戏后，网页会通过网页插件打开客户端插件，客户端插件根据注册表`HKEY_LOCAL_MACHINE\SOFTWARE\GAMANIA\MapleStory`下的`Path`项内容(默认为游戏文件夹)启动游戏.
> 
> 本程序的`Setting.exe`会将正确的游戏目录放到`GamePath`中，将`Path`修改为本程序的目录，这样客户端插件尝试启动游戏时，就会先启动本程序，然后本程序读取正确的游戏目录后，加载转区程序再启动游戏。

### 文件说明

- `MapleStory.exe`程序主体，客户端插件启动它之后，它会加载转区程序之后启动游戏。
- `Setting.exe`程序设置软件，主要为修改注册表用，为方便使用所以使用的有界面程序，如果你会自己修改注册表，也可以不使用它。
- `LRHookx64.dll`和`LRHookx32.dll`[Locale_Remulator](https://github.com/InWILL/Locale_Remulator)转区软件的关键文件。