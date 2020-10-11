# 项目使用说明

## 依赖

项目依赖MiraiCSharp框架,通过Mirai HTTP API提供的接口与Mirai间接访问

其他依赖包括:

- Newtonsoft.Json

- HowSimpleObject

## 功能概述

HowCrystal是一款有一定扩展性(对程序员来说)的QQ机器人。直接提供的功能是B站直播提醒。

## 项目结构

HowCrystal项目将MiraiCSharp包装起来,并实现了一些插件,使其变得不灵活,对普通用户不友好,扩展能力降低,但可能给亲手写机器人逻辑的用户提供便利

Crystal负责将MiraiCSharp进行封装(该封装并不严格,留了一些属于MiraiCSharp的数据结构在外面),提供比较容易的开启、关闭、事件处理方案。

CrystalPlugin抽象类以及继承自它的插件们是实际执行工作的部件,你可以建立新的插件,然后将插件进行注册使用。

EhowCore负责将各部分组合起来。一方面,负责间接接管Crystal的开启关闭,另一方面,维护所有插件。维护每一个插件需要用户亲手在EhowCore中调用RegisterPlugin进行注册。

LiveServer是一个包含了直播通知插件和直播通知服务器的部分,直播通知插件依赖于直播通知服务,所以在EhowCore中,我不仅调用了直播通知插件的构造函数,还调用了直播通知服务的构造函数,还为直播通知服务专门写了操作页面。

## 项目使用方式

HowCrystal需要生成和调试,需要您使用VS2019,如果不使用此IDE,可能需要自己手动配置项目,本人并不清楚其中的技术细节,不在此阐述。

HowCrystal需要执行,需要您提供

- 您开启的MiraiHTTP服务的服务器信息以及希望操作的QQ的号码。文件位于 `可执行文件所在目录\HowCfg\服务器设置.hso`。文件格式为纯文本,修改文件时需要遵循HSO的规范,否则无法被读取。其中涉及的配置项目及其意义在文件内有注释描述,不在此赘述。

- 满足项目中使用的所有插件的条件。这些条件会在下面按不同插件分别阐述。目前只有直播通知插件。

### 直播通知插件

直播通知插件是可以配置观测目标和通知目标的插件。这些配置是从文件读取的。

因此,插件要求您的程序可执行文件所在目录必须包含下述文件(夹)

- 直播观测目标文件,路径: `可执行文件所在目录\HowCfg\直播观测目标.hso`,文件内含注释。

- 直播观测目标文件,路径: `可执行文件所在目录\HowCfg\直播服务注册者.hso`,文件内含注释。


