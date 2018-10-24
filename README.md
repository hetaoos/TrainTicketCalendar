# Train Ticket Calendar 

自动识别[Gmail邮箱](https://mail.google.com/)中的**12306订票通知邮件**的车票信息，并保存到[Google 日历](https://calendar.google.com/)中

## 大概工作流程

- 初始化12306数据并保存到 [LiteDB 数据库](https://github.com/mbdavid/LiteDB) 中，并周期性更新
  - 12306接口 [RailsApiService](./TrainTicketCalendar/Services/RailsApiService.cs)
  - 车站信息 [StationService](./TrainTicketCalendar/Services/StationService.cs)
  - 车次信息 [TrainService](./TrainTicketCalendar/Services/TrainService.cs)
- 使用[OAuth2方式](https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth)登录谷歌账户
- 使用 [Gmail API](https://developers.google.com/gmail/api/) 定期检查邮箱中发件人为**12306@rails.com.cn**的邮件
- 解析邮件中的 **订票、改签、退票** 信息，并通过12306的接口获取车次的详细信息（到达时间、站台等），最后保存到数据库中。
- 调用 [GoogleCalendarService](./TrainTicketCalendar/Services/GoogleCalendarService.cs) 服务中的日历处理方法，从数据库中查询未处理的订票信息，使用 [Google Calendar API
](https://developers.google.com/calendar/) 根据车票的状态生成或删除日历
  - 订票：生成新的日历
  - 改签：找到并删除之前的日历，再创建新的日历
  - 退票：找到并删除之前的日历
 
## 使用方法
### 关于凭证文件
>credentials.json

Google OAuth2 登录要用到的，默认就好，当然，你也可以到[Google API Console](https://console.developers.google.com/)自行生成一个，需要启用 Gmail 和 Google Calendar。

### 配置文件 [appsettings.json](./TrainTicketCalendar/appsettings.json)
```json
{
  "settings": {
    "passengers": [ "张三" ],  //乘客姓名，一般填写自己的名字;为空则将所有人的车票信息都生成到日历中
    "start_date": "2018-01-01",//初始化时，从这天开始查找12306的邮件，并处理
    "calendar": {
      "name": "铁路行程", //日历本名称，也就是将生成的日历放到这下面
      "reminders": [     //提醒信息
        {
          "method": "popup", //提醒方式，目前之前 sms/popup/email, 其中 sms 及允许 G Suite 使用。
          "minutes": 70 //提前多少分钟通知
        },
        {
          "method": "popup",
          "minutes": 30
        }
      ]
    },
    "proxy": {  //代理设置，只支持 WebProxy
      "mode": 1, //代理启用方式：0或者null，禁用；1，仅在 Google 服务启用代理；2，仅在12306接口启用代理；3，全部启用代理。
      "address": "http://127.0.0.1:7070", //代理地址
      "username": null, //用户名，留空则不用验证
      "password": null  //密码
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Tickets.db" //数据库保存地址，默认就好
  },
  "Logging": {  //日志相关，默认就好
    "IncludeScopes": false,
    "LogLevel": {
      "Default": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Windows 用户
### 生成可执行文件
>dotnet build -c Release -r win-x64

### 注册成 Windows 服务
假设编译输出的文件夹全路径是 %OUTPUT_PATH%
>sc create TrainTicketCalendar start=auto binPath="%OUTPUT_PATH%\TrainTicketCalendar.exe --service"

在启动服务之前，必须先双击运行 TrainTicketCalendar.exe ，并在弹出的浏览器窗口中完成OAuth2授权。