#服务的启动方式,以grpc方式启动+身份验证



## 区块链Service服务项目

该项目作为区块链节点项目，可以启动多个项目进行动态配置连接。

## raft网络leader选举 

基于raft共识的算法

## client&peer身份认证

**peer**

​	通过节点的身份配置加入

**client**

​	通过请求peer节点，获取token，使用token与节点进行通讯

​	client节点只能同发放token的节点进行通讯

## 区块数据同步



## 网络配置



