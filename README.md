[![.NET](https://github.com/he1a2s0/FuckCalibri.net/actions/workflows/dotnet.yml/badge.svg)](https://github.com/he1a2s0/FuckCalibri.net/actions/workflows/dotnet.yml)

### 原项目：

- https://github.com/zhmjx/FuckCalibri

### 本项目：

- 使用.net(NET Framework 4.6)实现

- 启动后添加托盘图标

- 添加关于窗体、添加当前状态显示

### 已知问题：

- 不能检测是否已生效（是否已覆盖内存），在找不到匹配的内存字节序列时，会当作已生效（因此使用不支持的onenote版本时，会出现状态显示已生效但实际并未生效的情况）
