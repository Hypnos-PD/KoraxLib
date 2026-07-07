---
layout: home
title: KoraxLib
titleTemplate: Slay the Spire 2 敌人模组前置库

hero:
  name: KoraxLib
  text: 面向 Slay the Spire 2 的敌人模组基础能力
  tagline: 注册敌人与遭遇内容，观察敌人生命周期事件，并逐步提供更安全的原版敌人行为复用方式。
  actions:
    - theme: brand
      text: 开始使用
      link: /zh/guide/getting-started
    - theme: alt
      text: API 参考
      link: /zh/reference/api-overview

features:
  - title: 敌人注册
    details: 声明 MonsterModel 和 EncounterModel 类型，由 KoraxLib 处理 STS2 ModelDb 与 encounter list 接入。
  - title: 生命周期事件
    details: 订阅 spawned、turn-starting、turn-started、dying、died 事件，不需要消费模组自己 patch STS2 hooks。
  - title: 已验证的运行时路径
    details: 内置 smoke encounter 已验证初始 spawn、死亡生命周期、胜利结算和存档序列化。
---

::: warning 早期 API
KoraxLib 仍处在 Milestone 1 阶段。敌人注册与生命周期事件已经存在；vanilla ability API 仍是计划中能力，尚未实现。
:::
