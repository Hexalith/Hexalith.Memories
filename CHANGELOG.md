# [1.2.0](https://github.com/Hexalith/Hexalith.Memories/compare/v1.1.0...v1.2.0) (2026-04-30)


### Features

* Complete retrospectives for Epics 8, 9, and 10; update sprint status ([e6d9e57](https://github.com/Hexalith/Hexalith.Memories/commit/e6d9e5764582709620a7449df5459d2680619451))

# [1.1.0](https://github.com/Hexalith/Hexalith.Memories/compare/v1.0.0...v1.1.0) (2026-04-26)


### Features

* Add retrospective and sprint change proposal for Epic 11 completion ([2fab295](https://github.com/Hexalith/Hexalith.Memories/commit/2fab2959cdf157e14e582374326fc880203e9eb9))

# 1.0.0 (2026-04-26)


### Bug Fixes

* **story-9.2:** apply 13 safe patches from adversarial code review ([9ee6601](https://github.com/Hexalith/Hexalith.Memories/commit/9ee6601511027056bbf1311f66d74c4dfaf1b64e)), closes [#pragma](https://github.com/Hexalith/Hexalith.Memories/issues/pragma)


### Features

* Add case member functionality with serialization tests ([319c5f8](https://github.com/Hexalith/Hexalith.Memories/commit/319c5f8023d1523258b7e2869d9c12c7541b4891))
* Add case-scoped search, cross-case attribution, and metadata filtering (Story 3.4) ([96bb15a](https://github.com/Hexalith/Hexalith.Memories/commit/96bb15a5e1671a1a6123a93b171bc8a70dbe51fb))
* Add DAPR configuration and tenant mismatch monitoring ([b33cd71](https://github.com/Hexalith/Hexalith.Memories/commit/b33cd7147c627c981cef39e29bcee9873c5d7b2c))
* Add EnvironmentTopicAttribute for dynamic topic resolution from environment variables ([efd97d3](https://github.com/Hexalith/Hexalith.Memories/commit/efd97d39c34ce21f63afa2098e66af5c03551862))
* Add memory graph model and serialization support ([2d8cd09](https://github.com/Hexalith/Hexalith.Memories/commit/2d8cd09d9ec147bcda9705b0f4b3418ef62b649d))
* Add PII Acknowledgment document for Story 9.2 dual-embedding pipeline ([83bb21a](https://github.com/Hexalith/Hexalith.Memories/commit/83bb21a4ee9996c2c2e090f3bd4021edfedc9cfa))
* Add search endpoint degradation logging and response handling ([948b8a5](https://github.com/Hexalith/Hexalith.Memories/commit/948b8a5508ffe9e45762e95848bd123aa01bd96f))
* Add search explanation metadata and serialization support ([a0d6e4b](https://github.com/Hexalith/Hexalith.Memories/commit/a0d6e4bcedb848e7da23e153facd71078c246cd3))
* Add tenant configuration and metrics functionality ([24f5ff7](https://github.com/Hexalith/Hexalith.Memories/commit/24f5ff7a3ba436fef5c7f412ae0eef958cbab1de))
* Add traversal and annotation models with serialization tests ([b8d8ea3](https://github.com/Hexalith/Hexalith.Memories/commit/b8d8ea3b783518790f23b18032c913550e5ba90f))
* Complete Story 9.3 by resolving final review findings and enhancing Redis observation logic ([bc4d5cc](https://github.com/Hexalith/Hexalith.Memories/commit/bc4d5ccfffa6d3fd9c3704f0ebed499139d62c72))
* Enhance case deletion process with status tracking and observability ([7c562f5](https://github.com/Hexalith/Hexalith.Memories/commit/7c562f5161036d7afd81ff2f0dedaf5df94632de))
* **export:** finalize data export implementation and documentation ([74b5dc4](https://github.com/Hexalith/Hexalith.Memories/commit/74b5dc46a271ef57f3c4b9b06f4b4213fe013f3b))
* Implement causal chain traversal feature ([2f63fc1](https://github.com/Hexalith/Hexalith.Memories/commit/2f63fc1d7edcbec219790171986ed8184881b257))
* Implement consistency inspection and repair workflows ([fe0811b](https://github.com/Hexalith/Hexalith.Memories/commit/fe0811bea88090d80c88eca3f769c6b7ae81e229))
* Implement CorpusStatistics actor for caching per-tenant RediSearch statistics ([2ecbbaf](https://github.com/Hexalith/Hexalith.Memories/commit/2ecbbaf209cb9cbee82aa6943af59d8f86b44864))
* Implement gap detection and confidence promotion for causal chains ([d20c0b2](https://github.com/Hexalith/Hexalith.Memories/commit/d20c0b2ea4b153ea75b615d4d8ace804575effc5))
* Implement GraphScopedSearch for traversing FalkorDB and enriching results from Redis ([81057a3](https://github.com/Hexalith/Hexalith.Memories/commit/81057a3923c56baff7598b6f47d5617ad74c0534))
* Implement ingestion and indexing activities with compensation and consistency checks ([fbd9c69](https://github.com/Hexalith/Hexalith.Memories/commit/fbd9c693060be7e18940e7594ce2e5051ed09fc0))
* implement ingestion workflow orchestration (Story 1.6) ([f1ae9d6](https://github.com/Hexalith/Hexalith.Memories/commit/f1ae9d663ae38925a02815a337108943349c599e))
* Implement Redis OTEL instrumentation and harden AC [#2](https://github.com/Hexalith/Hexalith.Memories/issues/2) from Story 8.4 ([d7495a3](https://github.com/Hexalith/Hexalith.Memories/commit/d7495a385ced342ec1dd137966bab0228a30cc6c))
* Implement Semantic Search Service with KNN vector search capabilities ([5c39312](https://github.com/Hexalith/Hexalith.Memories/commit/5c39312f93b952da0f74f8f6ca05d0d9a0ef5e87))
* Implement Syntactic Search Service with BM25 ranking and related data models ([0d104b7](https://github.com/Hexalith/Hexalith.Memories/commit/0d104b779d62b877f6fcfe51a0c262d3d4eaffff))
* implement three-backend indexing ([ed267d7](https://github.com/Hexalith/Hexalith.Memories/commit/ed267d7f5944066a1b19eea981cb991a8fd38875))
* Replace Apache Tika with Kreuzberg for content extraction ([f5d7a17](https://github.com/Hexalith/Hexalith.Memories/commit/f5d7a171ddfd97446b8321ea9f8a5f87285f1d26))
* **search:** add hybrid fusion ([#7](https://github.com/Hexalith/Hexalith.Memories/issues/7)) ([40b79fc](https://github.com/Hexalith/Hexalith.Memories/commit/40b79fcf04f731c5043a8a0f1ec6884a2a98cb76))
* **server:** add embedding generation workflow activity ([#2](https://github.com/Hexalith/Hexalith.Memories/issues/2)) ([b8e3bab](https://github.com/Hexalith/Hexalith.Memories/commit/b8e3baba878b99298f913ed7ab3023a76642dd08))
* **telemetry:** add FalkorDbSemanticAttributeProcessor to rewrite Redis tags ([1a7ddf8](https://github.com/Hexalith/Hexalith.Memories/commit/1a7ddf8909970b2e656224d28962cbb07196052a))
* Transition Story 9.2 to done and apply final review findings, including copyright header addition and eager startup probe enhancements ([f86eef1](https://github.com/Hexalith/Hexalith.Memories/commit/f86eef1fcba5d51eda1c6ab33c2191adac3efaec))
* Update framework setup progress and enhance test suite documentation ([1d8e3af](https://github.com/Hexalith/Hexalith.Memories/commit/1d8e3affb052f9fb5bdc3303701e3ee04072a22a))

# Changelog

Release notes are generated by semantic-release from Conventional Commits.
