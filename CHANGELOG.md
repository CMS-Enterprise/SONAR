## 0.1.0

### Align Environments page with mockups

* [view commit 65bf8f8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/65bf8f8daaac7dfb92ec017b042892d0d892d1fe)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 23 May 2023 20:20:51 +0000

```
Closes BATAPI-251

## Description:

* Update Environments page to align with mockups, implement expand/close all functionality.

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->

Closes #251
```

### BATAPI-207 Handle invalid service configurations gracefully

* [view commit 084ef61](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/084ef61cff2fadd405b82114838b505777e200d5)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 23 May 2023 15:44:58 +0000

```
Closes BATAPI-207

## Description:

Adds exception handlers around InvalidConfigurationException that log the error and swallow it.
```

### BATAPI-250: added font-family for static and dynamic text

* [view commit 3da3a0c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3da3a0cbf550cd1663a7945a4ba781b51b4bda95)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 22 May 2023 15:26:00 -0700



### BATAPI-250: added accordion container style

* [view commit d1ec469](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d1ec46915722cc0f27ae438b5b1b403c7010d57c)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 22 May 2023 13:52:13 -0700



### BATAPI-250 use emotion style objects, remove extra white space, add scroll

* [view commit fdcfd8d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fdcfd8dab41154e96ba537d9146afca4aadb0986)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 22 May 2023 13:10:28 -0700



### BATAPI-250 added health status badges component

* [view commit e48e7e4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e48e7e406ace022719b1a41231fc3134f613b633)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Thu, 18 May 2023 11:14:33 -0700



### BATAPI-250 updated light-mode color palette, toggle, and navigation bar

* [view commit 1b713e0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1b713e0cf474889753f3fb86bbcf129b0ac62da6)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Thu, 18 May 2023 09:06:13 -0700



### Refactored HealthCheckList and RootService/ChildService components

* [view commit 70035c6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/70035c674f34222e5b3d7b11f41fb274ceb0e409)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 18 May 2023 21:16:02 -1000

```
There were some pathological API queries being run in the
HealthCheckListItem component because data wasn't being queries and
passed to child components in an appropriate way.

Specifically each HealthCheckListItem needed the HealthCheckModel so
that it could display the health check definition. Each
HealthCheckListItem instance was fetching the complete configuration
hierarchy for the current tenant, and then selecting any health checks
with matching name. In addition to excess data queries this resulted in
the multiple health checks being displayed because health check names
are not globally unique within a service hierarchy.
```

### Fixed issue with SONAR Agent generated API client

* [view commit 86e0ac8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/86e0ac8e6aa5d236dee9604717658022006468b0)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 18 May 2023 21:11:08 -1000

```
Returning a collection of tuples from an API controller results in
generated code that expects a customed named type to exist to represent
the Tuple, but no such type exists in our model classes. As a result the
generated SONAR Agent client does not compile.

This change creates a custom type to wrap the collection of tuples
returned by the SONAR API. This pattern should be followed for future
API endpoints.
```

### Added unit tests for ServiceConfigMerger.

* [view commit 9eea9d8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9eea9d8fba8f2b23b8a57a61acd125f428b774dd)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 18 May 2023 14:39:28 -1000



### Increase agent's resilience to API outages, specifically in configuration loading (local and K8s)

* [view commit f6d4b16](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f6d4b1640d336002cf5134fe8aa9fe83fffc4198)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Sat, 20 May 2023 00:52:32 +0000

```
Closes BATAPI-198

## Description:

Add retry logic for config saving errors (locally and in-cluster).

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->

Closes #198
```

### sonar-agent: applied Dale's changes to support Kubernetes secrets.

* [view commit e8b70f4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e8b70f42cd712b9392264f87eb31076a2296599e)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 16 May 2023 23:39:05 -1000



### sonar-agent: Updated ConfiguHelper method naming to follow ...Async convention.

* [view commit 0c7e48e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0c7e48e94e41882236eaab3ac73690b00b80bf2e)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 15 May 2023 15:41:13 -1000



### BATAPI-230 merge request feedback.

* [view commit 3e6b00f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3e6b00fc2ecdfa78cff37fa79199c0f5e365b059)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 15 May 2023 15:39:22 -1000



### sonar-agent: added documentation comments explaining merge rules for ServiceHierarchyConfiguration

* [view commit ea8f206](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ea8f206d3084cf03b2562f651f924da0f7794b51)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 15 May 2023 14:59:22 -1000



### Refactored ConfigHelper and implemented merge semantics for service config

* [view commit 42c696e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/42c696ea985b5cdfd1b82dbeb2bd986be3b38f29)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 11 May 2023 09:32:23 -1000



### Added ability to get sonar configuration from kubernetes secrets.

* [view commit cf9e9b5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cf9e9b505f636fed5dea3885d5bc2105dc97e327)
* Author (Committer): Dale O'Neill (Paul Wheeler)
* Date: Wed, 17 May 2023 01:07:50 +0000

```
Closes BATAPI-229
```

### Add unit test

* [view commit 77a69ee](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/77a69eee0517f7bbd159a0e2e36d2caf461a2b0c)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 12 May 2023 11:32:20 -0700



### Add authentication to configuration controller, redact sensitive information if does not have correct permission

* [view commit f287a45](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f287a458427682f4b8db9458a468bf770f6944e4)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 10 May 2023 17:40:53 -0700



### Fix build error in TenantDataHelper

* [view commit f0b5ae3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f0b5ae3d842a7767f64e4bfac4a2036f5c74a9d7)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 12 May 2023 19:44:47 +0000

```
## Description:

* fix build error in TenantDataHelper

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->
```

### BATAPI-212 Resolve all warnings in sonar-api and sonar-agent

* [view commit 61f05bb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/61f05bbbcf86fd62a280867bec21c526ae06252a)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 12 May 2023 18:26:06 +0000

```
Closes BATAPI-212

## Description:

* sonar agent errors

* resolve warnings for sonar-api

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->

Closes #212
```

### BATAPI-242: Ensure strong API model validation is possible in sonar-agent

* [view commit fd4dda6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fd4dda6ac9dccbb3dc1b0fed2856cd8a62c64a68)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 12 May 2023 18:00:54 +0000

```
* Refactors API model record declaration syntax in order to support strong validation outside the context of the MVC framework (using System.ComponentModel.DataAnnotations.Validator) while also keeping compatibility with the MVC framework serialization/validation.
* Moves higher-order service hierarchy config validation logic into `IValidatableObject` interface; this enables that validation to happen automatically without the need to call an extra validation method.
    * Refactors code in the immediate blast radius of the preceding two changes.
* Adds a recursive wrapper around System.ComponentModel.DataAnnotations.Validator that can be used to validate an entire object tree.
    * Adds tests for this new class.
* Adds [Required] attributes to all non-nullable API model fields.
```

### cleaned up exception handling.  Do not catch all exceptions, just specific exceptions we are looking for.

* [view commit 19a7be6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/19a7be67b0bb5376669a9174a24aa1d0715c9b93)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 11 May 2023 11:26:30 -0700



### For the RecordStatusAsync method, I Added additional exception handling and reported it to the logger.

* [view commit 4c9ece6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4c9ece634d55cd2f7ff4566fd49dbf4b70c495a3)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 8 May 2023 14:49:00 -0700



### Revert "Upon startup, if sonar API is not running, the agent tries to send tenant configuration to Sonar Central, fails, and shuts down Sonar Agent.  This change allows the agent to stay up regardless of any connection errors."

* [view commit 83bee97](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/83bee97e25c734305c38a056088c7340f04fda08)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 8 May 2023 13:52:43 -0700

```
This reverts commit 359098c6a817c19e856a2f1973f4d95588ee3bb2.
```

### Fixed logging messages to not use any string interpolation

* [view commit d2c5dc7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d2c5dc71b6b110f90ab0f828526fd2968d25d2de)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 8 May 2023 13:50:56 -0700



### Sonar Agent exits when it tries to send metric data to Sonar Central unsuccessfully. The scheduler task throws exceptions when Central server is down.  This change allows Sonar Agent to continue working even when the Agent cannot communicate with Sonar Central.

* [view commit b0e9a69](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b0e9a69e54bbf8b1d1bb22e7f83e2fca4060104b)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Fri, 5 May 2023 13:17:08 -0700



### Upon startup, if sonar API is not running, the agent tries to send tenant configuration to Sonar Central, fails, and shuts down Sonar Agent.  This change allows the agent to stay up regardless of any connection errors.

* [view commit d1150c2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d1150c25e7ad8f8137201f3ba59de2efc258d45d)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Fri, 5 May 2023 13:07:33 -0700



### Emotion Refactor

* [view commit 60a0f98](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/60a0f985dba23049e2934b774fa1c9bdc1d20ab3)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 11 May 2023 06:38:35 +0000

```
Closes BATAPI-243

## Description:

Refactor current components to use Emotion styling.

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->

Closes #243
```

### fixed vulnerabilities related to libcrypto and libssl in or docker containers.

* [view commit b1ebecc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b1ebeccbc99cb86a4a538bebd70346f1f598c42d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 9 May 2023 13:13:25 -1000

```
Closes BATAPI-244
```

### Resolve #240 "Emotion style standards"

* [view commit 1bc3311](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1bc331182f8c02e431de67c76df0539e6268b6b8)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 9 May 2023 22:51:52 +0000

```
Closes BATAPI-240

## Description:

* Install and set up Emotion, add standards to README
* Implement Emotion styling for EnvironmentItem component

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->

Closes #240
```

### Implement reactQuery to fetch data

* [view commit 0f10945](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0f10945b8ec18b54ac6873f1be6716f5b975a5e2)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 5 May 2023 09:35:09 -0700



### Resolve unique react keys

* [view commit 19ceb73](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/19ceb73ed366257d834d6b9563586fca9babf22a)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 2 May 2023 22:41:47 -0700



### Add loki recording metrics

* [view commit 34fc38f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/34fc38f1e74a7a73cb002d137e69e4d0cc2451d6)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 2 May 2023 16:21:20 -0700



### BATAPI-215: Unify service config validation

* [view commit 817e060](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/817e060918bbaaba9f4f4e389d1a722079cf51b9)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 5 May 2023 22:04:17 +0000

```
Closes BATAPI-215

## Description:

* Moves ServiceHierarchyConfiguration validation logic directly into core domain model class.
* Refactors sonar-agent and sonar-api to use shared validation logic.
    * Except for one issue I found with the Json serializer (see [ConfigurationHelperTests comment](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/merge_requests/78/diffs#1039384a8157d8cc6a54499fc6439f9bd084621b_0_25), and [BATAPI-242](https://jiraent.cms.gov/browse/BATAPI-242)).
* Introduces a new InvalidConfigurationException core type and uses that in place of InvalidOperationException config validation errors.
```

### ran dotnetformat to bring up to standard.

* [view commit 71b1957](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/71b1957c91108793a7483b9fbd0b3a150dc173c4)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 4 May 2023 11:46:21 -0700



### No conversion to Localtime.  Left all calls to the API in UTC

* [view commit 39c364e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/39c364e06a4805d7c6f4e81ea486ae10a45ecd96)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 4 May 2023 11:11:47 -0700



### Resolved 6 out of the 7 PR comments.  Small items nothing major.

* [view commit 7fab813](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7fab813a1d7f0d5073f26b05a16b5f0c49117863)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 4 May 2023 10:40:49 -0700



### Ran dotnet format to clean up code to standard

* [view commit fe79812](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fe798129b38539bdae4ef106dc8b885d7db8f2c6)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 3 May 2023 11:48:15 -0700



### Seperated root and root/child services test

* [view commit 37dfab1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/37dfab17a3f84a772eeb3dffa5d36c849c71d5e5)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 3 May 2023 11:25:38 -0700



### Clean up code

* [view commit d786bbd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d786bbd266482bc3fa67fcbfc1ae98bb976dd0c7)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 3 May 2023 11:03:00 -0700



### Added root and child service testing

* [view commit 5f26287](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5f26287c5f7db397eeea121ac0d37e6ed117570b)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 3 May 2023 10:43:46 -0700



### Created Health History Controller unit test.  Three tests created

* [view commit 42e91cd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/42e91cdc023e826b7831af10c818f06825032dc4)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 2 May 2023 16:38:57 -0700



### Make sure the resource is created before disposing of it.

* [view commit 161b9ed](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/161b9ed3536fb679407d94583bb46ce3852e5418)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 2 May 2023 16:34:47 -0700



### Implement React Query Library for all async operations

* [view commit 88f2661](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/88f2661a40d1eedac55d66805ae68441841f14b4)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 3 May 2023 04:14:51 +0000

```
Closes BATAPI-193

## Description:

* implement react query for async data fetching, fix error with DateTimeHealthStatusValueTuple

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->

Closes #193
```

### Add fix to StatusHistoryTile

* [view commit 256f05e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/256f05ee96b13bc1dfa506cd48d633a3779b8b1a)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 1 May 2023 22:00:17 -0700



### Additional comments and revert data-contract

* [view commit b252c3a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b252c3a6f6835fb496ff72855da76eda9d53aa29)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 1 May 2023 17:12:28 -0700



### Resolve Lint

* [view commit b62097a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b62097a4650c1f362197b0ca9245e3264ed3d4a2)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 1 May 2023 16:58:37 -0700



### Update to README

* [view commit 9dce536](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9dce536bb192ff33c7b15275d3c12c2031b20c73)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 1 May 2023 16:51:18 -0700



### Resolve lint and address PR comments

* [view commit c46ba6d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c46ba6d2d5cd97d43799cef47829e1bdc67f1c41)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 27 Apr 2023 16:09:34 -0700



### Displaying annotations

* [view commit c19e87b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c19e87bf87ed1ba5c4543fa80702ff93562b5f58)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 27 Apr 2023 11:20:53 -0700



### WIP useEffect to populate annotations

* [view commit 3b34e50](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3b34e50d35b35092c19e351b85c3bbcd153a0ceb)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 26 Apr 2023 11:21:01 -0700



### Refactor threshold display

* [view commit 01a2183](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/01a218348d1af42a632c3e7a372c4465eb83724c)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 25 Apr 2023 23:27:02 -0700



### address comments from PR

* [view commit 59f5ea0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/59f5ea02b7682394d46c5601c3e0d3e071d35248)
* Author (Committer): btakushi (btakushi)
* Date: Mon, 1 May 2023 08:42:51 -1000



### reformat

* [view commit bf35d39](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bf35d39c3ec76be9901f98993c644549b0861bf8)
* Author (Committer): btakushi (btakushi)
* Date: Fri, 28 Apr 2023 11:01:42 -1000



### finish implementation, static step for history query

* [view commit 1560893](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1560893008325264cb0859eee76541c2edb5a893)
* Author (Committer): btakushi (btakushi)
* Date: Fri, 28 Apr 2023 10:57:55 -1000



### css refactor

* [view commit 1441943](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1441943f7603570989a97164c5b1285a3ac1ec46)
* Author (Committer): btakushi (btakushi)
* Date: Mon, 24 Apr 2023 09:32:40 -1000



### separate tile class

* [view commit c752af0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c752af04dd80d90154fa24d54387144c133e8773)
* Author (Committer): btakushi (btakushi)
* Date: Tue, 18 Apr 2023 09:17:58 -1000



### add icons to tiles

* [view commit 0d7d4e7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0d7d4e7eab836a727654644465bf9f4167b88fdc)
* Author (Committer): btakushi (btakushi)
* Date: Thu, 13 Apr 2023 18:00:09 -1000



### WIP: ui complete without live data

* [view commit 94962bc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/94962bcae841bfdd0a7d7b203b64ae36422cbd56)
* Author (Committer): btakushi (btakushi)
* Date: Wed, 12 Apr 2023 09:40:35 -1000



### WIP

* [view commit a845291](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a845291e2fe0c43b725f87c3dc95a044980fa7f0)
* Author (Committer): Kevin Ly (btakushi)
* Date: Mon, 3 Apr 2023 12:56:16 -0700



### Intial Commit for Tenant Component

* [view commit 5b5f078](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5b5f078c7a576372b1d0969f9fb32f2c283ea7a8)
* Author (Committer): Kevin Ly (btakushi)
* Date: Thu, 30 Mar 2023 16:19:16 -0700



### Added unit tests for ReportingMetricQueryRunner

* [view commit 84e3448](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/84e344803356b84acdf2ba5434dd5c9d030669b5)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 26 Apr 2023 21:21:21 -1000

```
Added exception handling and logging for ReportingMetricQueryRunner
```

### sonar-agent: Report metrics data to SONAR API for metrics health checks

* [view commit 6e0f767](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6e0f767228dd9e36a68de7351696e2cd043cc444)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 26 Apr 2023 14:23:47 -1000

```
Closes BATAPI-195
```

### sonar-agent: updated SONAR API client

* [view commit d1a9ceb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d1a9ceb94eda272081cc95e59a42e186ff781466)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 26 Apr 2023 14:23:34 -1000



### Refactored HealthCheckHelper to use injected/singleton processors.

* [view commit 6b9b7cb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6b9b7cbe4220712e4f6f7f8e72646c74d38a1aa5)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 24 Apr 2023 11:18:09 -1000

```
 * Previously a set of HealthCheckQueueProcessors was instantiated for
   each Tenant, which mean that health checks were not throttled across
   tenants.
 * Also refactored the way health check name information is passed to
   the queue and health check evaluator components.

Closes BATAPI-220
```

### Added new controller to perform a history of the metrics.  This controller is...

* [view commit d5e7082](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d5e70823b2ace534f62722f839b9b8ea6be1c652)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 27 Apr 2023 23:18:30 +0000

```
## Description:

Implemented health history controller to return the historical health status of services, or the service hierarchy for a tenant.

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [X] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->
```

### Feature 196 fetch health check metric data endpoint

* [view commit 7d1b740](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7d1b740af8ce5afbbe998aa2be2d92c331200eb7)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Thu, 27 Apr 2023 01:00:42 +0000

```
Closes BATAPI-196

## Description:

Fetch a single metric time series for a specific health check. The caller should optionally be able to specify a start and end date. The default end date should be the current time in UTC and the default start date should be ten minutes ago in UTC.

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->
```

### Adds MSBuild target to run unit and integration tests with coverage and generate an HTML code coverage report (housekeeping item, no associated issue number).

* [view commit 4ec80b3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4ec80b301032f213d62eb63879ed0e284a7c6736)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 25 Apr 2023 21:22:49 +0000



### Removed all test files from the repo

* [view commit c03761d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c03761d9f9d978e4f41c9c15c8a33cc3ef6d2544)
* Author (Committer): Mark Valdez (Paul Wheeler)
* Date: Mon, 24 Apr 2023 19:53:32 +0000

```
Closes BATAPI-223
```

### Create component style.tsx, update readme

* [view commit 412623c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/412623c3d422002da04647b3d51a1ecc4cb2b82b)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 24 Apr 2023 11:21:14 -0700



### Update time windows, tooltip, and resolve lint issues

* [view commit b8a4000](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b8a400095f9993162fdd9e2345c703dce5c26091)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 19 Apr 2023 14:16:29 -0700



### Pass tenant and environment name to child component, migrate charts and table to serviceView

* [view commit 828412e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/828412e1b90452fe902c5a82f4dc83ad50c6f14d)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 19 Apr 2023 11:59:31 -0700



### Mockdata and table

* [view commit 7694c7a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7694c7af76cc1ec5bce1d755477c46136485035f)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 18 Apr 2023 11:27:51 -0700



### WIP Chart Options

* [view commit 65f2707](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/65f27075dde580a8f707c4af0b627327f3fa4d29)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 13 Apr 2023 13:06:33 -0700



### Init commit

* [view commit 4882609](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4882609a7caaae7f076bd50a2a2a0165d00e9abc)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 12 Apr 2023 20:31:08 -0700



### Tests for functionality added in BATAPI-194

* [view commit f7452a6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f7452a60ad3ffa8618aac68101c3a2c9a43db540)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 24 Apr 2023 15:31:04 +0000

```
Closes BATAPI-214

## Description:
* Adds PrometheusRemoteProtocolClient tests
* Adds PrometheusService tests

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.
```

### Added support for multiple version to our OpenAPI specs.

* [view commit 8a095a0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8a095a0d66d564f24d43d69e4de73861e648cf4a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 18 Apr 2023 23:10:20 -1000

```
Allso updated the Swagger UI configuration so that it handles the
multiple versions and is enabled in all environments.

Closes BATAPI-213
```

### Added integration tests for LegacyController

* [view commit 3ff4ef4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3ff4ef4cafbe879cf14d4aa6ceb42aaa13eba2c6)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 14 Apr 2023 10:01:42 -1000



### Fixed lint issues

* [view commit 0726390](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/07263900c6ee22fcbeaa839830340f9ad25de842)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 13 Apr 2023 08:58:19 -1000



### Implemented backward compatibility endpoint.

* [view commit 4a72c9c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4a72c9cd5490b2ea59cccdc8914635c9515983da)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 7 Apr 2023 09:35:37 -1000

```
Closes BATAPI-206
```

### Added API versioning.

* [view commit 49f845b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/49f845bc3396634f25e0b7f9d83fd3d091dc20a1)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 6 Apr 2023 17:51:32 -1000



### 66 display health check status conditions

* [view commit 28a099c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/28a099c6b51aa5a8017668ba971986017ab489d7)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 17 Apr 2023 18:31:54 +0000

```
Closes BATAPI-66

## Description:

* Display list of health status conditions configured for a service. List of health checks and health check conditions shows up as expandable control within each service in the ServiceView page.
* In the service list view, when the user clicks on a specific health check they should see a list of conditions in a human readable format.
* For metric health checks the details should also include the metric query used.

## Security Impact Analysis Questionnaire

### Submitter Checklist
-  [ ] Is there an impact on Auditing and Logging procedures or capabilities?
-  [ ] Is there an impact on Authentication procedures or capabilities?
-  [ ] Is there an impact on Authorization procedures or capabilities?
-  [ ] Is there an impact on Communication Security procedures or capabilities?
-  [ ] Is there an impact on Cryptography procedures or capabilities?
-  [ ] Is there an impact on Sensitive Data procedures or capabilities?
-  [ ] Is there an impact on any other security-related procedures or capabilities?
-  [x] No security impacts identified.

## Security Risks Identified:

<!-- For any applicable items on the "Submitter Checklist," describe the impact of the change and any implemented mitigations. -->
```

### Adds /usr/local/lib to shared library search path for MacOS + homebrew installed libraries

* [view commit ea928fb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ea928fb0bfde92ec11ccc5e52453dbb2a651288f)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 13 Apr 2023 13:21:20 -0600



### Updates quoting in ToString override

* [view commit 45a0903](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/45a0903a2d3140ecd8d2f330a6dbfefc309b7226)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 13 Apr 2023 09:36:32 -0600



### Updates log message

* [view commit c6dbc94](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c6dbc941b38d36af0d0fd7268d26f55160f10013)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 19:39:18 -0600



### Cleans up whitespace

* [view commit 6beb9cc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6beb9ccfbebf07a479dd88d325d82e9b38feb803)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 19:36:01 -0600



### Updates log message

* [view commit 8df34de](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8df34de0930f758e1f9c2904ed97eb2bc04c29a2)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 19:34:49 -0600



### Updates debug log message

* [view commit 686c8b5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/686c8b58f744d30821e78e0f1d098b9678ced511)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 19:30:50 -0600



### Updates endpoint handler name to conform to ubiquitous language used throughout the rest of the controller; adds authz handler; decreases verbosity of debug log messages

* [view commit 000d261](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/000d261848459c5af5771641c5efef751ea017eb)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 16:37:10 -0600



### Updates log statement wording

* [view commit c91f2f5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c91f2f513d35456f0e250b168e90938c4877b482)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 16:02:44 -0600



### Fixes inconsistent indentation

* [view commit 5cd0b4d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5cd0b4d52fc5c3c3868dbc4dd545162f66f05848)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 16:01:27 -0600



### Adds additional test parameter to postman collection

* [view commit 56f6f50](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/56f6f50c6f10f0f4007c05ec2cb1bcd47f4d1402)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 12:07:04 -0600



### Fixes error in postman collection

* [view commit 6866385](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/68663859664c1cbcf837ad6a382343bbcac2d1d2)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Apr 2023 11:37:31 -0600



### Update log message format

* [view commit 178a7bf](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/178a7bfb5dd8af5e4c8c8f12fdf3d88fa89e025f)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 17:19:55 -0600



### Adds one-hour TTL filtering

* [view commit c08c255](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c08c255b366ffd5aff057c2a70a0e66b2058a366)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 17:13:18 -0600



### Cleans up imports and updates doco

* [view commit 884db6c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/884db6c325368d4a14fb7ab59c44e8bcda1ea144)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 16:48:31 -0600



### Adds post-transaction logging

* [view commit 3e5417c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3e5417c23d6c2248b972b82d005a0cd6d6d17ec6)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 16:45:32 -0600



### Adds return value to API endpoint

* [view commit 1cafe1d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1cafe1dbdfd9537a24db8d0ab9c2d5e58b0be6d7)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 16:42:11 -0600



### Adds doc comments

* [view commit 12fd2fb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/12fd2fb7cc7ff34a1cdfc56db95cd9a0d5c1ed9d)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 16:24:54 -0600



### Changes separator in PrometheusClient::ToPrometheusDuration to empty string (the space was causing prometheus to puke)

* [view commit a3f16ac](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a3f16ac8e790dd112bd5a8f4a05031ea47dfc4e3)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 15:48:14 -0600



### Limit timestamp check query to minimum necessary time range, up to 1 hour

* [view commit c80c226](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c80c226a1268c18c3f5bdf1860f638ce403d9183)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 14:39:27 -0600



### Refactoring

* [view commit 84bca1b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/84bca1babe5f0855b672d6e10b8b7a89a20ee241)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 14:14:10 -0600



### Renames request in Postman collection

* [view commit dd32011](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dd320119d3874e7be126f060f312970b8ddda63d)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 12:36:15 -0600



### Fixes accidental double reference in csproj

* [view commit 89ec9dc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/89ec9dc3afa5181ef4f8896e3d257a7668e76cfe)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 12:30:08 -0600



### Resolves merge conflict

* [view commit 6991b1b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6991b1b0ce79e70121ce8a63c94ce888f250b9e0)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 12:09:42 -0600



### Resolves merge conflict

* [view commit 5df5362](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5df5362f4ba130cf349697e00f5518e55bf0be4e)
* Author (Committer): Paul Wheeler (Stephen Brey)
* Date: Wed, 5 Apr 2023 14:44:21 -1000



### Adds business logic for filtering stale samples

* [view commit 7ac2d43](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7ac2d43099be2883fa066b96e33fb3730a2eda7b)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 11 Apr 2023 12:05:28 -0600



### Refactoring

* [view commit 5a7fcd6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5a7fcd6d954a1c26020d51c6f43060ce1bd558c9)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 6 Apr 2023 17:38:55 -0600



### Adds new endpoint to postman collection

* [view commit 365210a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/365210ac261b536987c978b2b2e736fb1a620869)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 5 Apr 2023 17:30:43 -0600



### Adds endpoint for reporting raw health-check metric data and accompanying DTO

* [view commit ff25592](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ff25592602b456de2fd1057fa935408ea1b5b05f)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 5 Apr 2023 16:21:18 -0600



### Adds string constant for raw health-check data metric name

* [view commit 30cedc7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/30cedc7801ee47fbc527eae008756fd7b9b55639)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 5 Apr 2023 16:20:31 -0600



### Adds DateTime extension method for converting to millis since the unix epoch

* [view commit 7e5ca2f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7e5ca2f2f8580c99bdc2620eb28580cf3304bff2)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 5 Apr 2023 16:18:22 -0600



### Resolve lint and remove unused code Add css var for commonly used colors Resolve PR findings

* [view commit fb45aab](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fb45aabd7fd6075f20c4dd16057e44657b055a81)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 5 Apr 2023 15:00:37 -0700



### Update readme

* [view commit 74ed25b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/74ed25b32bb0d218fac00d6d1925ed96cbaaea19)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 5 Apr 2023 14:43:38 -0700



### Remove autogenerated code

* [view commit d95b5a5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d95b5a5c6917c9a9b0c54d500c1b8cf20786074a)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 5 Apr 2023 14:41:08 -0700



### Icons and Color

* [view commit 8e3e475](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8e3e475acde2e1c45292f620f09311c0952a19d1)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 5 Apr 2023 14:27:18 -0700



### Intial Commit for Tenant Component

* [view commit d53244a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d53244a88d9bd6cb1dcae2cb92e1c3905738fc95)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 30 Mar 2023 16:19:16 -0700



### Update our test kubernetes manifests to disallow privilege escalation in all pods.

* [view commit b9b7036](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b9b7036978135e6f9f5217e94f277145fdcda51c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 5 Apr 2023 14:45:35 -1000



### Resolve vulnerabilities resulting from Snappy pulling in an old version of NETFramework.

* [view commit d979692](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d9796927156cc74ffa290e9bb374e511e1914a76)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 5 Apr 2023 14:44:21 -1000



### Improved docs in ConfigurationHelper

* [view commit e2266d2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e2266d2e9128ccd39bee5dbccf0e6059a7eb5891)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 20 Mar 2023 15:08:28 -1000



### Fixed whitespace issue in KubernetesConfigurationMonitor.cs

* [view commit 0504cbe](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0504cbe47a8e63206033fdf57f12bc49e680d994)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 4 Apr 2023 15:27:34 -1000



### 176 watch Kubernetes API and auto-update configuration changes

* [view commit 36c152a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/36c152a1f93a79a2e8fff9be5aa107fe4b1923aa)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 3 Apr 2023 19:25:39 +0000



### Fixes whitespace linting error.

* [view commit cff04d3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cff04d31591a8307d51a3736bdbb447bcd44f42b)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 31 Mar 2023 22:34:14 +0000



### 191 .NET 7.0

* [view commit 2e9dca1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2e9dca11aa65f6eb24b9ae0590d02f077ea2fd4d)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 31 Mar 2023 19:53:17 +0000

```
Updates projects target frameworks and relevant dependencies to .NET 7.0, updates the README for snappy installation issues I encountered on my Apple silicon Mac, and fixes a broken unit test.
```

### Add Postman get requests

* [view commit 70ab941](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/70ab9412ed9bc9b2b3f5ab788f098485ef63aac5)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 30 Mar 2023 15:38:05 -0700



### Resolve PR findings

* [view commit 239e650](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/239e650a866b43c60ef38fe13b72abec21608779)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 30 Mar 2023 15:04:10 -0700



### Create tenant and update Environment endpoint. Add ata helpers and models for tenant and environment health. Add health data helper form healthController.

* [view commit bb80b3e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bb80b3e5fb0e8205ef1f5efc326a607e2ec897f6)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 16 Mar 2023 22:24:43 -0700



### Initial commit

* [view commit 190e1bd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/190e1bd94ceffc44ae9461d6f256157054bf2ce1)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 10 Mar 2023 11:31:02 -0800



### sonar-ui: Addressed merge request suggestions for environment list.

* [view commit 51ea622](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/51ea622b1f456b03c35cc8eacd7906ded05d5f99)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 28 Mar 2023 12:03:04 -1000



### sonar-ui: fixed eslint issues.

* [view commit dec0df8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dec0df8c4168b29ff25051e2850f14538c0ffc44)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 28 Mar 2023 01:34:13 -1000



### sonar-ui: add comments and readme

* [view commit ad58668](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ad586681f7f03976c66258c7ff0de401f489ced8)
* Author (Committer): btakushi (Paul Wheeler)
* Date: Tue, 21 Mar 2023 13:58:30 -1000



### sonar-ui: initial implementation of environments listing

* [view commit 1899324](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/18993245a5efb8a7057248db9f7916dd56071fc8)
* Author (Committer): btakushi (Paul Wheeler)
* Date: Tue, 14 Mar 2023 07:33:31 -1000

```
TODO: use actual env and tenant endpoints once developed
```

### sonar-uu: Addressed code review suggestions for service list.

* [view commit c696b35](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c696b3522ec5f3b3ad752471ff05d1676d18722d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 27 Mar 2023 23:47:36 -1000

```
 * Enabled eslint
 * Added npm script for generating the SONAR API client
 * Improved the OpenAPI schema generated for tuples
 * Added sonar-ui specific editorconfig file
```

### sonar-ui: add routing for service list

* [view commit c1db885](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c1db88584734d2288cae457486a4d1330ae9e6c4)
* Author (Committer): btakushi (Paul Wheeler)
* Date: Mon, 13 Mar 2023 11:56:04 -1000



### implemented service list component

* [view commit b62642a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b62642a309a0d12537e610af7a238011ee0bfc7e)
* Author (Committer): btakushi (Paul Wheeler)
* Date: Thu, 9 Mar 2023 15:11:18 -1000



### Added missing image for sonar-api postman docs.

* [view commit 8f020c4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8f020c42a46f8ab15df2d5a458293e45de816cf7)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 27 Mar 2023 09:31:18 -1000



### Updated Postman collection and added documentation.

* [view commit a9313b1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a9313b1635328c527d32a854c86a9c8a34894671)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 22 Mar 2023 18:05:47 -1000

```
Updated Postman colleciton with more requests and parameterization.
Added documentation in sonar-api/README.md
```

### Switched our Dockerfiles to use Alpine Linux.

* [view commit 04a407f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/04a407f1da6062f8517983f620989aefe670aa9a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 22 Mar 2023 19:29:19 -1000



### Added admin controller to enable database initialization.

* [view commit 4cd7da2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4cd7da20460a0413844ef1697296ade625490413)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 22 Mar 2023 21:59:31 -1000



### Fixed formatting issues.

* [view commit 14e4628](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/14e4628b68edebef298b2d0d9c7c96be4fe0b03d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 21 Mar 2023 15:47:45 -1000



### sonar-core: Added tests for RecordConfigurationExtensions.BindCtor and fixes issues.

* [view commit a7e3d84](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a7e3d849781ce527b516dabe5ace7cb55fbd5112)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 20 Mar 2023 19:01:09 -1000



### Added support for configuration types that have array or collection values.

* [view commit c695a2e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c695a2e73da0b5388732e66f7dcce7b5efe089a8)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 9 Mar 2023 16:27:02 -1000



### some tweaks to the sonar agent helm chart and some additional developer documentation.

* [view commit e7c6428](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e7c64289fc177f68d3e56c26a7b9e20ad8060d39)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 10 Mar 2023 22:55:51 -1000



### Added k8s resource configuration for installing SONAR API, dependencies, and test apps.

* [view commit 7e7dd5b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7e7dd5ba34bc4b8ceead64a70b5fdbe23edfa9e2)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 10 Mar 2023 22:52:48 -1000



### bug: fixed issue with tasks being disposed before completion.

* [view commit b9906f8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b9906f833cd74ba1311a4c323eff3dab8bf4bf61)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 10 Mar 2023 22:51:17 -1000



### bug: fixed issue with sonar-agent StatefulSet installation because of unquoted boolean value in environment variables.

* [view commit a3497fe](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a3497fe9e0d598a6e9ac40251eeaf67f02c00ac0)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 10 Mar 2023 22:50:21 -1000



### Add Error Exception and fix function name

* [view commit 0fd0f66](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0fd0f6607ba2a0c49ecb934628e4d38040986ce2)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 10 Mar 2023 11:47:52 -0800



### Fix response type

* [view commit 6502a13](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6502a13aaa45c262f2891e757f0a34bc7029fb42)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 9 Mar 2023 23:30:34 -0800



### Add endpoint for Environments

* [view commit 33d1fcb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/33d1fcb3aac59778a384036067ebc1c9605db64f)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 9 Mar 2023 22:33:36 -0800



### Created react app, generated api client, added absolute imports

* [view commit 3b5c765](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3b5c7650eac86b531f240792e3d834fc9c54ae21)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 10 Mar 2023 01:23:37 +0000

```
Closes BATAPI-186
```

### sonar-agent: added additional documentation comments.

* [view commit 09928af](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/09928af6f09d8a10c33f065438e900e3bfac7acd)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 7 Mar 2023 21:13:28 -1000



### sonar-agent: Added tests for HealthCheckQueueProcessor and adding trace and debug logging.

* [view commit 931ca19](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/931ca1950179e553afb15dc17cecbc858eb88e37)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 24 Feb 2023 18:58:49 -1000



### Added sonar-agent-tests project.

* [view commit 0ebf58e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0ebf58e1c716365f9bc82e5b404341c7745a264a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 23 Feb 2023 23:56:24 -1000



### sonar-agent: Added documentation comments to health check classes.

* [view commit 2efda66](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2efda667927ba09122acc350a4da54a0f343eeaf)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 21 Feb 2023 22:22:15 -1000



### Ripped out the Future class because I missed that it is in the framework and called TaskCompletionSource

* [view commit f072801](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f072801a783c55a17c304ff9d5dd68481c56759c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 24 Feb 2023 18:10:45 -1000



### sonar-agent: Implemented parallel health check execution.

* [view commit d190692](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d19069295c1d3eec8f44d358ae0b0df3d208a230)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 16 Feb 2023 15:11:09 -1000

```
Closes BATAPI-165
```

### Refactored Prometheus and Loki HealthCheckDefinitions to be the same type.

* [view commit c4414bc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c4414bca64ac4a89cc0ff530b9783b4c10fdd242)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 17 Feb 2023 21:34:36 -1000



### fixed lint issues

* [view commit 4053f1c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4053f1cd323f77f1470e09893c459b98be343272)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 7 Mar 2023 21:36:33 -1000



### Multi-tenant Health Checks

* [view commit 8a1b884](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8a1b884d0f670885b48120450ed7d39591469e0d)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 8 Mar 2023 04:01:32 +0000



### 181 incluster config

* [view commit 5326d65](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5326d6550be18cbe99c274677ff3e266304e4424)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 7 Mar 2023 04:29:36 +0000



### Renamed sonar-core-test project and updated namespace for consistency.

* [view commit 677565d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/677565ddbc6469f101e80a3dab5ae614507deabf)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 23 Feb 2023 23:28:12 -1000



### 184 environment-specific API key

* [view commit 91f827f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/91f827fd035309e1dc9e725ac32b6168d9f58f2e)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 22 Feb 2023 21:19:39 +0000



### BATAPI-174 kubernetes config

* [view commit 4a1a04c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4a1a04ca2b57511b1ebcdf98c62ce3e8f024b15f)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 22 Feb 2023 01:47:47 +0000

```
Closes BATAPI-174
```

### BATAP-88 automatic config reload in the agent

* [view commit a91041e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a91041ef79b92534ce6451f12a981a68ff82a09c)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Thu, 16 Feb 2023 19:00:14 +0000



### sonar-agent Helm Chart Release v0.0.3

* [view commit 7b55f44](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7b55f4480fbaac01aacbb575c5ff8d09cf83d0eb)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 13 Feb 2023 23:46:14 -1000



### Refactored the sonar-agent Helm Chart's service config values.

* [view commit 0f3f341](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0f3f3411f716ea9658073ee36aad5e1cc85b0b17)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 7 Feb 2023 21:18:51 -1000

```
Service configuration is now specified with dicts instead of arrays for
configs, services, and healthChecks.

This allows configmaps that are used as sources for these values to be
merged with better semantics. In short, if a configmap for a particular
environment needs change just one property of one particular health
check it can specify just that one property, instead of having to
replicate all of the rest of the configuration. Health check conditions
would still have to be replaced all or nothing because they are not
uniquely identified by some key.

Also made some tweaks to the way some appsettings values were layed out
and the config checksum generation.
```

### Correct the ApiKey environment variable for sonar-agent.

* [view commit e541b0f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e541b0f0ab5f3eec63658a64cb032c411eb47865)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 2 Feb 2023 15:37:27 -1000



### BATAPI-88 automatic config reload in API

* [view commit 0cc87d0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0cc87d0ca278677da6d7b81535d02e7414a3b84b)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 15 Feb 2023 21:17:12 +0000


## 0.0.3

### sonar-agent: eliminated redundancy in naming for properties on MetricHealthCondition

* [view commit 5267bf9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5267bf9a24567ecfcdf9f0f8520ca29c1d4fab6c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 14 Feb 2023 18:09:03 -1000


## 0.0.2

### address PR comments

* [view commit 0c36506](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0c365064a33b48893bc2abfd733bf7b565cbca10)
* Author (Committer): btakushi (btakushi)
* Date: Mon, 6 Feb 2023 15:56:16 -1000



### run caching operation synchronously

* [view commit 8b37b65](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8b37b65b9c42d6103b512ab4a3d2c7c8100368b9)
* Author (Committer): btakushi (btakushi)
* Date: Thu, 2 Feb 2023 14:24:13 -1000



### finish implementation

* [view commit 9d0b7ff](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9d0b7ffac34737e858b548fb1c6f62827c8ce12a)
* Author (Committer): btakushi (btakushi)
* Date: Thu, 2 Feb 2023 09:37:24 -1000



### begin implementation

* [view commit 4bd888b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4bd888bbcaf5013fb0ce421c73c4ccb95d41ab0d)
* Author (Committer): btakushi (btakushi)
* Date: Thu, 2 Feb 2023 08:56:06 -1000



### Fixed lint issue.

* [view commit c2e506d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c2e506d2a9c51412d7eec95b751016b3b37ad9dc)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 7 Feb 2023 22:27:17 -1000



### sonar-agent: added an option to the HttpHealthCheckDefinition to ignore certificate validation issues.

* [view commit f0748cb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f0748cbc95a7ed85664ac48583054906acf12d64)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 2 Feb 2023 17:55:14 -1000

```
Also changed the default behavior of sonar-agent http health checks to follow redirects.
```

### Fixed transient test failure due to incorrect datetime format string.

* [view commit 68519fa](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/68519fae72152cdee12223680eb174bf07066c4b)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 31 Jan 2023 15:32:56 -1000



### sonar-agent: improved error messages when required configuration is missing.

* [view commit fcf0646](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fcf0646067787923ce77df5096a32ebe82e9868a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 31 Jan 2023 15:31:15 -1000



### Make the description field for health checks optional.

* [view commit 054d54f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/054d54fa702b47b1442afbe75a07c5ef70184254)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 31 Jan 2023 16:07:54 -1000



### Added missing SonarHealthCheck section to the sonar-api-tests appsettings.

* [view commit 884c8a7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/884c8a75b4202646c12156f87778ea9802a868c7)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 2 Feb 2023 11:17:21 -1000



### sonar-agent: made service config validation order independent.

* [view commit bb84395](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bb843955f48cc8a2cd991c4237bbda2df4e2c74d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 31 Jan 2023 16:39:23 -1000

```
Also...
 * throw InvalidOperationException not OperationCanceledException for
   invalid config
 * check service names for uniqueness
 * deserialize service hierarchy directly from the input stream
 * Since service configurations are validated independently, merge root
   services list instead of replacing. Otherwise you cannot preserve the
   root services from the base config.
```

### Tweaks to sonar-agent command line arguments.

* [view commit 6136cc7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6136cc7a317f68812ec9609310e3aa4cd4ef027a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 31 Jan 2023 15:22:30 -1000



### Misc tweaks to sonar-agent logging and HTTP healthcheck behavior.

* [view commit d3c7f85](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d3c7f857ba9d9b275968bd90a562f31eb080a84a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 30 Jan 2023 22:56:02 -1000

```
* Default to Offline if not conditions are met
* Include a default StatusCode in [200, 204] if no other conditions
* Added support for the FollowRedirects and AuthorizationHeader settings
* Fixed a bug in the caching logic
```

### Add appsetting dictionary to statefulset

* [view commit 942d38d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/942d38d2ab35ea8f0620ff77a120ffbcae25234d)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 2 Feb 2023 15:32:07 -0800



### Specify sonar-agent command line arguments correctly

* [view commit ae0ece9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ae0ece9e04be70d04837ff97f00e5ab01f34f01f)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 31 Jan 2023 14:30:48 -1000

```
(each entry is a separate array enry)
Updated sonar-agent version to 0.0.2
Updated command line switch to be --appsettings-location
```

### Added a template for creation of a container registry secret.

* [view commit 519d369](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/519d3690d2f75d8d3fd36e05935d04c9e4985646)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 27 Jan 2023 16:03:51 -1000

```
Previously it was necessary to pre-create a secret with registry
credentials in the namespace that this helm char was being installed in.
This additional template allows the credentials to be provided as part
of the values and creates the secret as part of the chart. This mimics
the behavior of BigBang charts.
```

### Cleaned up some aspects of the sonar-agent helm chart values.

* [view commit e7fae5e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e7fae5e579058444295797a46a8f36f618a41b34)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 27 Jan 2023 16:08:25 -1000

```
Added support for additional elements of the Loki and Prometheus configuration.
```

### finish implementation

* [view commit 2cd3cf0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2cd3cf0b71fae7ced235006146a277bd6665c622)
* Author (Committer): btakushi (btakushi)
* Date: Tue, 24 Jan 2023 16:06:03 -1000



### begin implementation

* [view commit bf53ad5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bf53ad5f6aa2428916479253896787af541524cf)
* Author (Committer): btakushi (btakushi)
* Date: Tue, 24 Jan 2023 09:16:26 -1000



### Resolve PR findings

* [view commit 0852215](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0852215946b87ae5e04121627681c915c943405a)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 27 Jan 2023 11:05:39 -0800



### Update README.md

* [view commit 4224f77](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4224f77e0d4744248a6e0364957859a9be80a4cb)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 25 Jan 2023 03:17:58 -0800



### Create k3d deploy script

* [view commit 4f88304](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4f8830470fce675d6f72a4879f10f4c8f47e1db1)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 25 Jan 2023 02:52:45 -0800



### BATAPI-141 Add logging to sonar agent

* [view commit a50180e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a50180efdb4f4ec0bdc95fcfde0457ec9b5d1a39)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Thu, 26 Jan 2023 00:29:25 +0000



### remove typo

* [view commit 6b8982c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6b8982cd8d9a422836ee664267e2f7fc105182d7)
* Author (Committer): btakushi (btakushi)
* Date: Wed, 25 Jan 2023 12:36:14 -1000



### remove db username

* [view commit ba586fd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ba586fd70c75c2de526ee1959e8d3ce50a89078e)
* Author (Committer): btakushi (btakushi)
* Date: Wed, 25 Jan 2023 12:32:47 -1000



### address code review comments

* [view commit 3cfb00f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3cfb00f7b914050a9f856ff3d37e756633644c28)
* Author (Committer): btakushi (btakushi)
* Date: Wed, 25 Jan 2023 09:28:59 -1000



### add agg status calculation

* [view commit 82705d3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/82705d3de1455c4016d4cb8df9e4db7fe00a09bc)
* Author (Committer): Paul Wheeler (btakushi)
* Date: Tue, 24 Jan 2023 17:37:50 -1000



### implement postgresql health check

* [view commit 1574c1c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1574c1ccfeb8792c3fbf8dc9f8cf64914bb9199c)
* Author (Committer): btakushi (btakushi)
* Date: Mon, 23 Jan 2023 15:03:35 -1000



### self check endpoint implemented, TODO: postgres self check

* [view commit 8510fbc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8510fbcc98618db5fc4b76fca95d416c46e106bd)
* Author (Committer): btakushi (btakushi)
* Date: Wed, 18 Jan 2023 16:55:43 -1000



### Minor formatting fix in sonar-agent/Program.cs

* [view commit 9bd0621](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9bd0621f99fb26196d84ee5724c15754378326eb)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 24 Jan 2023 20:35:50 -1000


# 0.0.1

Initial release.
