import { defineConfig } from "vitepress"

const repoBase = process.env.GITHUB_ACTIONS === "true" ? "/KoraxLib/" : "/"

export default defineConfig({
  base: repoBase,
  cleanUrls: true,
  description: "A Slay the Spire 2 enemy modding library.",
  lang: "en-US",
  lastUpdated: true,
  title: "KoraxLib",
  locales: {
    root: {
      label: "English",
      lang: "en-US",
      themeConfig: {
        nav: [
          { text: "Guide", link: "/guide/getting-started" },
          { text: "Reference", link: "/reference/api-overview" },
          { text: "Development", link: "/development/roadmap" }
        ],
        sidebar: {
          "/guide/": [
            {
              text: "Guide",
              items: [
                { text: "Getting Started", link: "/guide/getting-started" },
                { text: "Enemy Guide", link: "/guide/enemy-guide" },
                { text: "Lifecycle Events", link: "/guide/lifecycle-events" },
                { text: "Smoke Testing", link: "/guide/smoke-testing" }
              ]
            }
          ],
          "/reference/": [
            {
              text: "Reference",
              items: [
                { text: "API Overview", link: "/reference/api-overview" },
                { text: "EnemyRegistry", link: "/reference/enemy-registry" },
                { text: "EnemyEvents", link: "/reference/enemy-events" },
                { text: "Enemy Plugins", link: "/reference/enemy-plugins" },
                { text: "Vanilla Abilities", link: "/reference/vanilla-abilities" }
              ]
            }
          ],
          "/development/": [
            {
              text: "Development",
              items: [
                { text: "Roadmap", link: "/development/roadmap" },
                { text: "Architecture", link: "/development/architecture" },
                { text: "M1 Spec", link: "/development/spec" },
                { text: "Lifecycle Investigation", link: "/development/enemy-lifecycle-investigation" }
              ]
            }
          ]
        }
      }
    },
    zh: {
      label: "简体中文",
      lang: "zh-CN",
      themeConfig: {
        nav: [
          { text: "指南", link: "/zh/guide/getting-started" },
          { text: "参考", link: "/zh/reference/api-overview" },
          { text: "开发", link: "/zh/development/roadmap" }
        ],
        sidebar: {
          "/zh/guide/": [
            {
              text: "指南",
              items: [
                { text: "开始使用", link: "/zh/guide/getting-started" },
                { text: "敌人与遭遇", link: "/zh/guide/enemy-guide" },
                { text: "生命周期事件", link: "/zh/guide/lifecycle-events" },
                { text: "Smoke 测试", link: "/zh/guide/smoke-testing" }
              ]
            }
          ],
          "/zh/reference/": [
            {
              text: "参考",
              items: [
                { text: "API 概览", link: "/zh/reference/api-overview" },
                { text: "EnemyRegistry", link: "/zh/reference/enemy-registry" },
                { text: "EnemyEvents", link: "/zh/reference/enemy-events" },
                { text: "敌人插件", link: "/zh/reference/enemy-plugins" },
                { text: "原版能力", link: "/zh/reference/vanilla-abilities" }
              ]
            }
          ],
          "/zh/development/": [
            {
              text: "开发",
              items: [
                { text: "路线图", link: "/zh/development/roadmap" },
                { text: "架构", link: "/zh/development/architecture" },
                { text: "M1 规格", link: "/zh/development/spec" },
                { text: "生命周期调查", link: "/zh/development/enemy-lifecycle-investigation" }
              ]
            }
          ]
        },
        docFooter: {
          next: "下一页",
          prev: "上一页"
        },
        darkModeSwitchLabel: "主题",
        langMenuLabel: "语言",
        outline: {
          label: "本页目录"
        },
        lastUpdated: {
          text: "最后更新"
        }
      }
    }
  },
  markdown: {
    lineNumbers: true
  },
  themeConfig: {
    footer: {
      message: "KoraxLib is an early-stage Slay the Spire 2 library mod.",
      copyright: "Released by the KoraxLib contributors."
    },
    search: {
      provider: "local"
    },
    socialLinks: []
  }
})
