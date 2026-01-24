import { DocsThemeConfig } from 'nextra-theme-docs'

const config: DocsThemeConfig = {
  logo: <span style={{ fontWeight: 700 }}>Orleans.Search</span>,
  project: {
    link: 'https://github.com/TGHarker/TGHarker.Orleans.Search',
  },
  docsRepositoryBase: 'https://github.com/TGHarker/TGHarker.Orleans.Search/tree/master/docs',
  head: (
    <>
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <meta property="og:title" content="TGHarker.Orleans.Search" />
      <meta property="og:description" content="Full-text and indexed search capabilities for Microsoft Orleans grains" />
    </>
  ),
  sidebar: {
    defaultMenuCollapseLevel: 1,
    toggleButton: true,
  },
  toc: {
    float: true,
    backToTop: true,
  },
  editLink: {
    content: 'Edit this page on GitHub',
  },
  feedback: {
    content: 'Question? Give us feedback',
    labels: 'documentation',
  },
  footer: {
    content: (
      <span>
        MIT {new Date().getFullYear()} - TGHarker.Orleans.Search
      </span>
    ),
  },
}

export default config
