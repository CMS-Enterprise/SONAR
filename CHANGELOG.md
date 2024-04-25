## 0.7.0

### Feature Maintenance

* [view commit 4df6b3a7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4df6b3a723721c871d7683295d31c64b13872c38)
* Author (Committer): Stephen Brey (Blaise Takushi)
* Date: Thu, 25 Apr 2024 19:53:14 +0000

```
Closes BATAPI-652

## Description:

Adds environment/tenant/service maintenance tracking to SONAR
```

### update OpenTelemetry.Instrumentation.AspNetCore to 1.8.1

* [view commit 436bb0be](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/436bb0be0afca9f65bb864b62bda1728231627bc)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 16 Apr 2024 03:20:14 +0000

```
## Description:

* update OpenTelemetry.Instrumentation.AspNetCore to 1.8.1 as recommended by GitHub advisory database
## 0.6.4

### Display service version info in side panel

* [view commit f4bace8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f4bace888bf49b3754191ae6e0bc1be1dda9b69c)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 9 Apr 2024 18:44:33 +0000

```
Closes BATAPI-418

## Description:

* When the user clicks on a tile in the health history, the panel that appears on the right hand side should display version information for the service at that time.
* Also updated `VersionHistoryController` to use `ValidationHelper`'s `ValidateTimestamp` function.
```

### Resolve occasional connection already open bug

* [view commit af0cb8a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/af0cb8a0efd7855356548a392771ef0729fb59f3)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 8 Apr 2024 18:41:50 +0000

```
Closes BATAPI-658

## Description:

* resolve race condition where two tasks try to open a db connection

Closes BATAPI-658
```

### SONAR Agent Helm Chart Release v0.6.3

* [view commit 611e5b7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/611e5b76bd03b05b5a46eadd8ab74786f0e55290)
* Author (Committer): Blaise Takushi (Stephen Brey)
* Date: Thu, 4 Apr 2024 20:51:57 +0000



### Update model, additional code refactor, update return type, add unit tests, update postman

* [view commit 592a8da](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/592a8da4aea062260288a79a4c8ce0f2bc5d25cf)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 25 Mar 2024 23:29:46 -0700



### Initial commit, working health history output, refactor health history

* [view commit ad22045](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ad22045073fd42d65dcf881d791aad4df5840d5f)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 20 Mar 2024 18:11:50 -0700


## 0.6.3

### Resolve BATAPI-660 "Sonar test schema bug"

* [view commit 9279d9d4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9279d9d49633350838cc22d42b3037ffe758c70d)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 2 Apr 2024 23:10:29 +0000

```
Closes BATAPI-660

## Description:

* implement workaround using migrationshistoryschema

Closes BATAPI-660
```

### BATAPI-661: fixed ProduceResponseType and regenerated the UI and Agent autogen code

* [view commit 128a83a2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/128a83a2d8f9775df0544d4804cf55e5a904e730)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 26 Mar 2024 10:34:36 -0700



### BATAPI-417: added API controller and tests for fetching the version history of a service or tenant's services

* [view commit 978f3424](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/978f3424f7d632600a41f294df926575cdfde6e0)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 25 Mar 2024 17:11:30 +0000

```
Closes BATAPI-417

## Description:

* Add API controller to fetch historical time series of a version for a service or a tenant's services.
## 0.6.2

### fix bug for internal health checks not displaying in status drawer

* [view commit 437deaf6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/437deaf6a87d82bf562f38011316fc3711c20c24)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 20 Mar 2024 19:48:54 +0000

```
## Description:

* fixes bug for internal health checks not displaying in status drawer
```

### Net8 Migration

* [view commit fcd011f0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fcd011f055fba81a551614d7142eab6b77841f78)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 20 Mar 2024 00:37:17 +0000

```
Closes BATAPI-527

## Description:

* Migrate to Net8. 2 breaking changes addressed:
  * ISystemClock is obsolete: https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/isystemclock-obsolete
  * Legacy serialization support APIs are obsolete: https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0051

Closes BATAPI-527
```

### SONAR Release v0.6.1

* [view commit 3f4d49b4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3f4d49b4294ec5699e3fa2f950534d302050a33a)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 19 Mar 2024 21:33:31 +0000


## 0.6.1

### Http response time data UI

* [view commit 665de43](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/665de43067745ae669d6ed33a6bb9b2312e0be5d)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 13 Mar 2024 00:34:30 +0000

```
Closes BATAPI-638

## Description:

* Implements chart for response time data
* Refactors logic in `HealthStatusDataTimeSeriesChart` to handle different types of metric data

Closes BATAPI-638
```

### Add maintenance status fields to entity health/info query response models

* [view commit 19305e0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/19305e07b4de71ea6f820299fbcfad708967277b)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 12 Mar 2024 20:51:07 +0000

```
Closes BATAPI-639
```

### Log additional debug information when creating errorReports on the API and warnings when unable to save errorReports to DB

* [view commit 1bf9503](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1bf95038cab2c939f2b264a5fc4c6c01b2698ea1)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 6 Mar 2024 19:05:08 -0800



### Add functionality to sonar agent to report HTTP response time metrics to sonar api

* [view commit e5036d0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e5036d0353e3139042792b60a9e249a4aa012137)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 8 Mar 2024 23:02:34 +0000

```
Closes BATAPI-637

## Description:

* Add functionality to sonar agent to report HTTP response time metrics to sonar api

Closes BATAPI-637
```

### BATAPI-585: dashboard ability to view a service's status history with different date/time ranges

* [view commit 1bf09bd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1bf09bde8113fc221c3da70aee812e8730bf754f)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 6 Mar 2024 22:49:35 +0000

```
Closes BATAPI-585

## Description:

* User has the ability to request a time range for a service's status history (quick range or custom range via date picker).
* User has the ability to shift the status history range forwards or backwards in time, depending on the current range in view.
* Up to ~20 status history tiles are displayed at a time in the current view.
* When selecting a custom status history range, the user must select a start date before an end date AND the user must select both a start and end date before being able to query
* NOTE: the earliest choosable start date was hardcoded to 15 days before the current date and time--this can be changed
```

### Adds deployment job for sonar-agent in dev environment

* [view commit 158ed60](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/158ed606cdf0f098f3e072953f246a812cbee171)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 21 Feb 2024 17:05:44 -0700



### Format Error Config Info

* [view commit 7a372fe](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7a372fe96a71b311db96e077aa57cd0d5ee44786)
* Author (Committer): Albert Tran (Albert Tran)
* Date: Wed, 28 Feb 2024 18:41:26 +0000

```
Closes BATAPI-487

## Description:

* Format Error Config Info
```

## 0.6.0

### Adds built-in alerting capabilities to SONAR.

* [view commit 2971f4a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2971f4a9e91cf5a11fe35a7e91011b5a9aba75b8)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 27 Feb 2024 17:55:55 +0000

```
Closes BATAPI-16
```

### Add condition to display chart

* [view commit 5a5c66b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5a5c66b7cff19c883deb32a8f8cfaf47b4fa62db)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 23 Feb 2024 10:08:14 -0800



### Add and populate dashboardLink property in ServiceHierarchyHealth and ServiceHierarchyInfo

* [view commit 7ecc918](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7ecc9186b88fbdfce4574a6b9b11ace09ad14792)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 22 Feb 2024 22:43:56 +0000

```
Closes BATAPI-586

## Description:

* Add and populate dashboardLink property in ServiceHierarchyHealth and ServiceHierarchyInfo

Closes BATAPI-586
```

### Updated timestamps for healthcheck

* [view commit 1284c29](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1284c293632b19d08a48fb23645ab68238a81f0f)
* Author (Committer): Albert Tran (Albert Tran)
* Date: Fri, 2 Feb 2024 20:28:55 +0000

```
Closes BATAPI-219

## Description:

* Updated timestamps for healthcheck
```

### Use max_over_time function to always return the worst status that occurs in an interval

* [view commit 64bff22](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/64bff22f7f18de096bbdc75056d8d2a04de00705)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 31 Jan 2024 21:28:09 +0000

```
Closes BATAPI-562

## Description:

* fix bug, implement regression tests

Closes BATAPI-562
## 0.5.7

### BATAPI-575 "Root agg status bug"

* [view commit 00b7ccd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/00b7ccd2dae444da48377986ea19ea274f14bad9)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 25 Jan 2024 19:14:12 +0000

```
Closes BATAPI-575

## Description:

* Fixes bug where a parent with no health checks (no determined health status) derived an incorrect status from its children. The issue was in the `ToServiceHealth` function where a parent with no health checks would take the first child's status (which is the reason for the non-deterministic statuses showing up in the dashboard on refresh).

Closes BATAPI-575
## 0.5.6

### Adds environment IsNonProd flag to the TenantInfo model

* [view commit 3c187bd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3c187bd5c70dbab5a4c64216f3a6f317c703a0f3)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 23 Jan 2024 00:26:31 +0000

```
Closes BATAPI-570
```

### Resolve BATAPI-227 "Use prometheus remote protocol client"

* [view commit 8cff25a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8cff25a6c58a60da552ed98070bdf85768674e49)
* Author (Committer): Albert Tran (Albert Tran)
* Date: Fri, 19 Jan 2024 22:37:26 +0000

```
Closes BATAPI-227

## Description:

This branch is to use Prometheus remote protocol client instead of Prometheus remote write client.
```

### Closes BATAPI-462

* [view commit 066283b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/066283bfee3f3e658ac2bf5fcd9ed2df70577f6b)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 8 Jan 2024 16:58:53 +0000

```
* Adds rules to sonar-agent k8s cluster role allowing read for healmreleases, deployments, stateful sets, and daemon sets
## 0.5.5

### Update readme, add execute beta2v2 on not found and logging, update unit tests

* [view commit c6477c3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c6477c30e01e858aee20911553040a10cf0d40ed)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 13 Dec 2023 14:28:49 -0800



### Add Flux HelmChart Version Check, update message, Resolve indentation, add serialization/deserialization, add versionchecktype, update k8s kustomization with appsettings, add support for helmrelease v2beta1 and v2beta2

* [view commit 3476638](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3476638dfda1602dd971814b6c22b8095be7048a)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 7 Dec 2023 14:48:23 -0800



### Add Kustomization Config

* [view commit 59a2512](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/59a25126bb6174f816705db1c1dbd10c8c7a8d0d)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 18 Dec 2023 16:24:22 -0800



### Set appsettings IPrometheusRemoteProtocolClient default to Warning Closes BATAPI-534

* [view commit 776191d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/776191dda2cff1a8725594fbfcb0a844d68e580b)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 19 Dec 2023 18:30:12 +0000



### Update README.md to add details about creating development builds

* [view commit c807961](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c807961d3d1654fcd5a91825a11452ea2ba3effa)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 15 Dec 2023 23:20:21 +0000



### Closes BATAPI-464

* [view commit b7cac36](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b7cac36bc5e8269169eb57c3a428fda919c1cf87)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 14 Dec 2023 03:38:06 +0000

```
* Kubernetes resource image version check
## 0.5.4

### Remove using statement from http client handler returned from factory

* [view commit 2b6413b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2b6413be18859c225223b90c6645b38c57d977fa)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 6 Dec 2023 16:00:40 -0700



### Revert my earlier change that attempted to use apk to remove busybox

* [view commit fb1c353](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fb1c353a9fe89b08e0cd7809a531ce8cfc85740e)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 5 Dec 2023 13:09:52 -1000

```
It turns out that several core packages depend on busybox which prevents it from being removed by APK, so manually removing the binaries is the only way
```

### BATAPI-533 Pin base image for the UI container to node:18.18-alpine3.18 to address pipeline failures

* [view commit 8fd3021](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8fd302122009e1a53cada2c1b1c75a16da328667)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 5 Dec 2023 22:42:41 +0000



### SONAR Agent Chart Release v0.5.3

* [view commit 667941e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/667941e499f3fc188f3765ee97c4ae4a29946426)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 30 Nov 2023 17:19:27 -1000



### sonar-agent chart: added support for tenant level tags.

* [view commit 3325d47](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3325d475fe2021424eaf7aeef0ca9ac26f2b9a70)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 28 Nov 2023 17:58:44 -1000



### BATAPI-533 Update all Dockerfiles to use Alpine 3.18

* [view commit 645fcc4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/645fcc410a578358db735b224877b006b026193f)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 5 Dec 2023 19:37:30 +0000

```
Update all Dockerfiles to use Alpine 3.18 to resolve findings in DefectDojo
## 0.5.3

## Enable dev builds for the SONAR UI.

 * [view commit 358efd4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/358efd4e2622eee8c2db1d402de59c12e7d08f47)
 * Author: Paul Wheeler <pwheeler@revacomm.com>
 * Date:   Tue Nov 28 23:45:54 2023 -1000


## Added a Maintenance HealthStatus and added validation to ensure it isn't misused.

 * [view commit 1bab0e9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1bab0e981044496a0c28483523e3f1ead8a28ef1)
 * Author: Paul Wheeler <pwheeler@revacomm.com>
 * Date: Wed, Nov 29 23:13:14 2023 -1000



## sonar-api: apparently binding to the IPv6 loopback address does not work in BatCAVE EKS clusters.

 * [view commit e4c42c5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e4c42c5dab37d62ae6f585b2886d2eb35655f0aa)
 * Author: Paul Wheeler <pwheeler@revacomm.com>
 * Date: Wed, Nov 29 00:32:25 2023 -1000



### Added support for ipv6 to sonar-api.

* [view commit e525b84](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e525b84ef1d439b9b3cf7df3d7dec648035fe5a6)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Sat, 18 Nov 2023 15:32:12 -1000



### Updated the environments CI variables to include the environments dir.

* [view commit aea1214](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/aea121410efbe9feb8aea6464300cb172a76aae3)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 24 Nov 2023 14:26:15 -1000

```
Also merged all the dev environment jobs into one and enabled
parallelism.
```

### sonar-api: Fixed signatures in DbMetrics interceptor that did not match the interface declarations.

* [view commit 9ae58f7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9ae58f795cef7025192b1ded7a10a7a02632939b)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 24 Nov 2023 11:52:15 -1000



### sonar-api: fixed DbMetrics regex for simplifying command text with guid arrays.

* [view commit ed35608](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ed35608d5019ccf1f3ff3fe4ff9f79adc2756ea7)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 24 Nov 2023 10:31:32 -1000



### sonar-api: Updates to DbMetrics based on code review feedback.

* [view commit e70f7cf](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e70f7cf4fbfe8d035c38fb0b5e30049d4fc1f4a3)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 24 Nov 2023 09:57:50 -1000



### Remove folder reference

* [view commit 4fb2931](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4fb29316e27435fabd7b0deae0787337197fd5a5)
* Author (Committer): Kevin Ly (Paul Wheeler)
* Date: Wed, 22 Nov 2023 10:44:27 -0800



### Create DbMetrics to meter queries as counters, propogate for reader, nonquery, scalar. Set up Prometheus config for local docker tests.

* [view commit 4f31d8d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4f31d8df3b053c6e7124773667a5dcd41523ca0c)
* Author (Committer): Kevin Ly (Paul Wheeler)
* Date: Thu, 2 Nov 2023 14:12:34 -0700



### Adds base OTLP dependencies for collecting profiling metrics Pins to latest stable versions of OpenTelemetry hosting extensions and console exporter Adds OTel tracing configuration to web application builder Adds asp.net core instrumentation to tracing configuration

* [view commit 32fc793](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/32fc79393941018d5967cc331a225e492504d0f5)
* Author (Committer): Stephen Brey (Paul Wheeler)
* Date: Tue, 31 Oct 2023 13:47:07 -0600



### Updated API tests to use the updated api key format

* [view commit d03bf67](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d03bf67ca5b945c44c00e30196a932c38b80293c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 21 Nov 2023 22:56:23 -1000



### Close BATAPI-500 Hide non production environments

* [view commit 737fec5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/737fec56fc5d0305c5feabbf682cabdb3e1befed)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 23 Nov 2023 00:40:41 +0000

```
* Add checkbox switch to hide non production

* Updated api contract to get latest changes to sonar api.
```

### Resolve BATAPI-489 "Unit tests for httphealthcheckevaluator"

* [view commit 5ae63d9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5ae63d9a86928d038664a3a9199ba19781c62c68)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 22 Nov 2023 22:22:03 +0000

```
Closes BATAPI-489

## Description:

* Implement Unit Tests for HttpHealthCheckEvaluator. Test the interactions between different health check conditions and corresponding http responses.
```

### BATAPI-504 Create tenant for error report if tenant does not exist

* [view commit 32e09a4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/32e09a4f1c83c8b0aba408cf09ac1aa6ef2d049a)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 22 Nov 2023 15:50:37 +0000



### Tags section to service interface

* [view commit 4255f82](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4255f82f3b59c3bca8cd6aed26d064c0b0ba9e48)
* Author (Committer): Blaise Takushi (Paul Wheeler)
* Date: Wed, 22 Nov 2023 08:05:30 +0000

```
* Adds service tags section to service card
* Updates sonar client

Closes BATAPI-498
```

### chart release 0.5.2

* [view commit 71f3c1a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/71f3c1ae390789f263fff0b941128fd1dd12d5c1)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 16 Nov 2023 09:29:24 -0800


## 0.5.2

### BATAPI-499 Isnonprod flag

* [view commit c9baa01](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c9baa0175992df071a5504232d8845c2eaae433d)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 16 Nov 2023 06:45:42 +0000

```
Closes BATAPI-499

## Description:

* Added isnonprod flag to values and updated documentation in the readme file

* From configuration check IsNonProd settings and update sonar api if needed.

* Updated the client API generated code from Sonar API NSwag

* Added Put to controller to update environment.

* Added IsNonProd to CreateEnvironment.  Supports backward compactability

* Added Db migration files for IsNonProd

* Added IsNonProd to the Environment datamodel.  Also updated migrations table.

* Added IsNonProd property to Environment
```

### Update ServiceConfigMerger to handle tags

* [view commit d8a10ff](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d8a10ffd81731348709cb28ae232f76fe01053a4)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 16 Nov 2023 06:30:16 +0000

```
Closes BATAPI-502

## Description:

* update ServiceConfigMerger to handle tags

Closes BATAPI-502
```

### Tag inheritance and resolution

* [view commit 6fef6d0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6fef6d05bce8b41da19dd8d6ebda79e3c16529bc)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 13 Nov 2023 23:09:22 +0000

```
Closes BATAPI-497
```

### Merge branch 'release-0.5.1' into main

* [view commit 4155bd6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4155bd683f6f08c78d2e7d6f6b0f073f6a85bc92)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 8 Nov 2023 14:33:27 -1000



### Updated README with release instructions for starting from a previous point in the git commit history.

* [view commit b891d70](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b891d7005d8637e121f1e61cd3bbe36e60e8c11a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 8 Nov 2023 14:32:36 -1000



### BATAPI-503: fix bug in CustomFormatter where exception messages were being treated as a format string.

* [view commit 9ee33e9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9ee33e9eb6206559e9a58d66e103b5b14357cf6d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 8 Nov 2023 11:56:44 -1000



### sonar-agent: Fix SonarClient code generation.

* [view commit eb6002e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/eb6002ec9c9e22800d86703a552f153cad173a7b)
* Author (Committer): Paul Wheeler (Dale O'Neill)
* Date: Wed, 8 Nov 2023 02:04:58 +0000

```
## Description:
NSwag code generation for SonarClient was creating invalid code because it does not recognize the C# Tuple type and expects a custom class to represent that. This change adds an explicit type mapping from the type described in or OpenApi spec to the C# Tuple type.

No Security Impact

## 0.5.1

### BATAPI-503: fix bug in CustomFormatter where exception messages were being treated as a format string.

* [view commit 2d620f2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2d620f2340af6066b8764f5b70c728071562fe7a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 8 Nov 2023 11:56:44 -1000


### SONAR Release v0.5.0

* [view commit 0daeb18](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0daeb18d543ba5b680ae4e98aaaf38f543394cde)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 7 Nov 2023 10:17:26 -0700

## 0.5.0

### BATAPI-483: add copy button for newly created API Key Ids

* [view commit 06b4753](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/06b4753d0d113a1f833e0066328ae4e5a1ed4456)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 31 Oct 2023 19:25:53 +0000

```
Closes BATAPI-483
```

### BATAPI-486: display button to create environment when user is logged in and is Admin

* [view commit 74cba0f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/74cba0f19fad51061d9ecccf5a3b950710f461e6)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 31 Oct 2023 16:53:58 +0000

```
Closes BATAPI-486

## Description:

* When on the home page, only display button to create new environment when user is logged in and has Admin permissions.
* When cursor hovers over the button, display browser-native tooltip describing what the button does.
```

### blur filter input on enter or esc

* [view commit d779b43](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d779b432505f7eee20e337855f074268522eeddb)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 30 Oct 2023 18:59:10 +0000

```
Closes BATAPI-454

## Description:

* blur filter input on enter or esc
```

### BATAPI-484: updated Error Reports hooks to include condition for useQuery

* [view commit 8a64125](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8a6412599f085eaa2c160d095dcd9e1ef5de49b0)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 30 Oct 2023 17:09:38 +0000

```
Closes BATAPI-484

## Description:

* Errors to appear in the JavaScript console on Environment and Tenant pages(401 unauthorized from error-reports controller), so add check if the user is logged in and is an admin before sending requests to retrieve list of error reports.
```

### implement alert context, trigger alert for redirect

* [view commit a13abe1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a13abe1ff66569c73f8feb3ce505def169b0aae8)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 27 Oct 2023 00:54:18 +0000

```
Closes BATAPI-473

## Description:

* implement alert context, trigger alert for redirect
```

### Shift k8 watcher to be disposed before kubeclient

* [view commit c8c1882](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c8c188279e98ffc1db96188c1af7167d6222452e)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 25 Oct 2023 11:44:59 -0700



### BATAPI-466 Display HTTP health check body conditions in drawer

* [view commit 9d2c636](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9d2c636a9d682b55c03faf2b71b8e082b09ddb36)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 26 Oct 2023 21:43:01 +0000

```
* Converts http health check conditions list to react component and adds HTTP body conditions to display with support for the noMatchStatus field
```

### Fixed issue where version info is not displayed in the root service list.

* [view commit b9e267d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b9e267dd5a1285efe9a2636e37f4b7c178e71758)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 26 Oct 2023 10:33:51 -1000



### Environments filter

* [view commit 2690d59](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2690d59fd2ffc825ef69a1c4b367ede8e3856185)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 26 Oct 2023 00:16:09 +0000

```
Closes BATAPI-454

## Description:

* finish implementation
```

### BATAPI-438 expected k8s watcher disconnection events

* [view commit dbd07a1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dbd07a137d306ad5719e1db7174d3f4695a308d9)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 25 Oct 2023 14:57:55 +0000

```
Updates K8s namespace & configmap watcher onError callbacks to log expected exception at debug level.
```

### Update sonar-api/README.md with an example of setting the environment name explicitly.

* [view commit 9ffb92b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9ffb92b8af12ec21b363ba2e45cba50a8e59cc37)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 25 Oct 2023 04:00:03 +0000



### add xml comments on controllers

* [view commit 0cfb3b3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0cfb3b3f6d6a0d048bd74e37e7bb884989f00f92)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 23 Oct 2023 19:21:22 +0000

```
Closes BATAPI-471

## Description:

* add xml comments on controllers
```

### BATAPI-451: Link to Error Reports page from and display error reports count in breadcrumbs

* [view commit 9c945db](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9c945dbfe8fbeca09f252171141effea9be1723c)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 23 Oct 2023 19:12:29 +0000

```
Closes BATAPI-451

## Description:

* Add links to Error Reports page from Environment details, Tenant details, and Service pages
* At the end of an Environment's, Tenant's, or Service's breadcrumbs, display a link to the Error Reports page (if user is authenticated and has admin permissions)
* The Error Reports page link should use a warning icon, display the number of error reports for the Environment/Tenant/Service, and use a browser-native tooltip that says "View Error Reports".
```

### Revert IHttpContextAccessor injection for deprecated API key format usage logging; it breaks database migrations

* [view commit 1580380](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/15803801f49ae95d44996ac380af7d3f3d894d7f)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 20 Oct 2023 18:39:19 +0000



### address lint error

* [view commit 28eae02](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/28eae02cde85689071a7b047bf6a01311c581518)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 20 Oct 2023 02:55:38 +0000

```
## Description:

* address lint error
```

### Cluster role kustomization resource

* [view commit 7225d47](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7225d4798919207934fe370a1adcd96e414660bc)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 20 Oct 2023 02:40:22 +0000

```
## Description:

* update to chart 0.4.1, add clusterRole to allow access to kustomization resources
```

### Log specific issues found when service configuration validation occurs. Closes BATAPI-458

* [view commit fed7d6c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fed7d6ca743cb08b156094a136864703f5cc0e6f)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 19 Oct 2023 22:49:36 +0000



### sonar-api: Log opaque API key details when the deprecated API key header format is used

* [view commit e9b53cf](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e9b53cf8a040c65814b39aefa3cecfe693db1813)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 19 Oct 2023 19:55:16 +0000



### Update appsettings.Development.json to use impl.idp.idm.cms.gov for SSO.

* [view commit 6f1af73](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6f1af732cec0a77bfcd3fc2a88236b62789db61b)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 16 Oct 2023 11:10:44 -1000



### Closes BATAPI-450

* [view commit c5596f9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c5596f9ce1c5d34e56aa6529c3ad7729a0d75bc8)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 17 Oct 2023 20:03:40 +0000

```
## Description:

Added the Error Reports page for an environment, tenant, or service, which displays a list of error reports between {start}} (defaults to {{UtcNow - 1d}}) and {{end}} (defaults to {{undefined}}), with the timestamp, level, type, message, and if available, configuration data and/or error stack trace.
```

### SONAR Release 0.4.0

* [view commit 6d8dc22](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6d8dc22914d0a6061c2fa033972557b664c12026)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 17 Oct 2023 18:36:46 +0000

```
* SONAR Agent Helm Chart Release v0.4.0
* SONAR Release v0.4.0
```

## 0.4.0

### Fail-fast validation for HttpBodyHealthCheckCondition

* [view commit b11a0b4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b11a0b49371fe0f3b74d04a95d77102a738277f3)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 16 Oct 2023 23:06:47 +0000

```
* Adds Path and Value validation for HttpBodyHealthCheckCondition so we can fail on these types of issues during the early validation process, not during health check evaluation
```

### BATAPI-442Updated http body condition checking for more flexibility

* [view commit cd9008e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cd9008eca2d7d3db400de7a43a7dd3bb47497145)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 16 Oct 2023 22:30:27 +0000

```
Closes BATAPI-442

## Description:
* Updated http body condition checking.
```

### Implemented support for fetching error reports for a specific tenant.

* [view commit 1626e94](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1626e94ec3591a3b839e610b6ff318ff51cf7367)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Sun, 15 Oct 2023 23:55:09 -1000



### simplify logic for start/end date params, fix bug

* [view commit c866957](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c8669577c5ada49103bdd5b58a5dea563210ffc8)
* Author (Committer): btakushi (Paul Wheeler)
* Date: Fri, 13 Oct 2023 12:05:10 -1000



### Resolve BATAPI-446 "K8s prometheus scraping"

* [view commit 74e2470](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/74e247038e92172135a9babb9a28f812af384685)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 16 Oct 2023 18:59:08 +0000

```
Closes BATAPI-446

## Description:

update chart to allow Prometheus to scrape our metrics endpoint exposed by sonar-agent
```

### BATAPI-443 Call expensive BCrypt verify operation less often

* [view commit 6e5298a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6e5298ad9cc76fc1656b7cc0564867f8d8195cd7)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 13 Oct 2023 15:57:30 +0000

```
* Adds caching find-and-validate API key by Id method so we don't call BCrypt verify every time we get an API key with Id
```

### Fixed some minor issues with the flattened service list in the Tenant page.

* [view commit c65544c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c65544cae54fcdcd5be155277bd7ff7c92926b3b)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 5 Oct 2023 11:08:12 -1000



### OM endpoint via OpenTelemetry

* [view commit 083af98](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/083af983e893b2af13189864862f62a4e76465f2)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 6 Oct 2023 02:45:43 +0000

```
Closes BATAPI-445

## Description:

Exposes OM endpoint via OpenTelemetry
```

### BATAPI-457 Guard around null conditions in health check definitions Equals and GetHashCode

* [view commit 869bc1b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/869bc1b5728ca26d77a088dd04ed32f8bba8db4e)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 5 Oct 2023 18:32:18 +0000

```
* Adds null guards around conditions field in Equals and GetHashCode methods of health check definition classes
```

### BATAPI-431 Environment and Tenant page, Bread Crumbs

* [view commit bf11f86](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bf11f86f0f85be77c5fb5bedbea7c55904e946b2)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 5 Oct 2023 16:32:13 +0000

```
Closes BATAPI-431

## Description:

* Update Breadcrumbs and implement environment and Tenant page

* update routes to environment and tenant page

```

### Reverted change to HttpBodyHealthCheckCondition.Value property in order to maintain backward compatibility.

* [view commit 23731c5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/23731c5e2f09c973bd5d7fc96c0ad69b9687c065)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 3 Oct 2023 09:33:42 -1000



### add validation for timestamps

* [view commit 57c0647](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/57c06473a247a799f24c3535aad107b972b260cd)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 2 Oct 2023 21:41:42 +0000

```
## Description:

* add validation for timestamps
```

### Update bread Crumbs with links to new routes Environment and Tenant Pages

* [view commit 48f4261](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/48f4261e3f74523933784a1307ff270c1a94d995)
* Author (Committer): Dale O'Neill (Paul Wheeler)
* Date: Fri, 29 Sep 2023 22:45:34 +0000

```
Closes BATAPI-272
```

### Enable cascading delete of ErrorReport when the associated Tenant or Environment is deleted.

* [view commit 27add87](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/27add87cf9b6d8631bedb5aa7d18228f1c476f07)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 26 Sep 2023 17:00:08 -1000



### Fixed issue where sonar-agent fails to begin monitoring a namespace.

* [view commit 0ff9a56](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0ff9a56076e160daf5e09eabf29759fe57e09e55)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 25 Sep 2023 16:13:02 -1000

```
When an existing namespace already has ConfigMaps with the
sonar-config: "true" label, and the namespace is modified to have the
sonar-monitoring: "enabled" label, SONAR Agent fails to begin monitoring
for that namespace.
```

### Fix swagger-typescript-api generation and generate latest API client

* [view commit e30e51d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e30e51dbf1e6ac95a801386371269ce9e1636b51)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 26 Sep 2023 22:51:46 +0000



### Update remove configSourceException and move into invalidConfigSource. Pass raw config into error exception. Add test for deserialize error Catch Exception due to invalid HealthCheckType

* [view commit de923f6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/de923f6f672240769cf4beea767c9d2851ede01a)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 20 Sep 2023 23:06:25 -0700



### Create basic error report for validation and serialization. Updates to configurationHelper.cs

* [view commit 04f8a48](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/04f8a48134cdbedc7bf88fe87c8b59b00501839f)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 15 Sep 2023 08:30:31 -0700



### implement unhandled exception handler

* [view commit b3ef9fc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b3ef9fc9d654bc920900c5b9e113e0c095011ff0)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 26 Sep 2023 03:42:44 +0000

```
Closes BATAPI-409

## Description:

* implement unhandled exception handler
```

### Added handling for invalid JSON returned from Prometheus query request.

* [view commit 47dc1ae](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/47dc1aee9cf56798284c61854c8a88f94880dbd0)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 25 Sep 2023 01:55:45 -1000



### sonar-agent: Added better logging for unhandled exceptions occurring in worker threads.

* [view commit 81dfaf5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/81dfaf5877c1cfc9d39f7d7c1c66938b1f1b60ad)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 21 Sep 2023 10:24:04 -1000



### BATAPI-430 New action tenant controller

* [view commit e5ef673](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e5ef6734158e11a913448bfee5468d08e0195831)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Sat, 23 Sep 2023 15:00:22 +0000

```
BATAPI-430 Added an additional action to the tenant controller.

Closes BATAPI-430

## Description:

* Add action to tenant controller.  Gets all tenants or one tenant for an environment.
```

### Historical health check data (UI and new endpoint)

* [view commit e3a85b1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e3a85b18d222c572062919e6d4ca0afaf0f11ce3)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Sat, 23 Sep 2023 02:22:29 +0000

```
Closes BATAPI-426

## Description:

Adds an endpoint that returns historical health check results at a given timestamp for a service. I chose to add an endpoint instead of appending this data to the ServiceHierarchyHealthHistory model to help performance, as we really only need to query this data when the status history drawer is opened by a user/a different Status History Tile is selected. Makes corresponding changes in the UI.
```

### Eliminate unnecessary 'undefined' version display in the UI.

* [view commit 00d5d7a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/00d5d7a56c394fc7c0bfaea472c2aa001215139f)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 22 Sep 2023 01:00:57 -1000



### BATAPI-387: Confirm ability to extract version information of deployed application

* [view commit 17fded0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/17fded0e845ab25ff2797552e2706c0872fe4ea2)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Fri, 22 Sep 2023 16:22:29 +0000

```
## Description:

* Added `sample-kustomization` folder inside the `k8s` folder, where it contains sample CRDs for GitRepository and Flux Kustomization along with a README on how to view the version check information for SONAR in a local K3D cluster.
* Added implementation of the IVersionRequester for Flux Kustomization version info.
* Added wiring of Flux Kustomization version info implementation into the version check loop.
* Added unit test for Flux Kustomization version info getter.
```

### add error reporting in HealthCheckHelper

* [view commit 1f5a234](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1f5a234569711bf519f008e77dd53cd5318603fc)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 21 Sep 2023 02:27:33 +0000

```
Closes BATAPI-408

## Description:

* add error reporting in HealthCheckHelper
```

### Resolve BATAPI-407 "Config error reports"

* [view commit 0ecc9d0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0ecc9d074c041383346fda4ce5e143d4bf94e92e)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 19 Sep 2023 23:40:43 +0000

```
Closes BATAPI-407

## Description:

* Implement error reporting for saving/fetching config exceptions.
* Moved helpers into Helpers directory
```

### Added gitleaks configuration so that non-secret Okta configuration doesn't cause false positives.

* [view commit e5bad12](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e5bad127fca1aa8389fca3dafe29ea4e092d2768)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 14 Sep 2023 13:41:29 -1000

```
 * gitleaks runs for each of our projects: sonar-api, sonar-ui,
   sonar-agent; so each run should ignore directories that are scanned
   in relation the other two projects.
 * Okta audience id, client id, and authorization server id are not
   secrets. Including these in our repo improves the developer
   experience because we can all test against the developer Okta
   instance.
 * Commit hashes aren't secrets, and generally speaking it is unlikely
   that secrets would be accidently added to markdown files.
```

### BATAPI-423 Display Service Version and update Service page to use React context.

* [view commit 07c2d81](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/07c2d81b2a1f9a7b780d0d68e687a04367cf1a16)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 18 Sep 2023 20:35:09 +0000

```
Closes BATAPI-423

## Description:
* removed some of the properties that were being passed down via props from component to component and replaced with react context.

* Display version information for each root service
```

### Sonar Agent Version Checking

* [view commit e5441a2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e5441a26f97b6ec034a86cdedc950d2bd8befbf6)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Sat, 16 Sep 2023 00:41:26 +0000

```
* SONAR Agent version checking loop implementation
* HTTP-type version checks implemented.
```

### Resolve BATAPI-405 "Error report controller"

* [view commit 842879c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/842879cffd1a144432d68520d9f66ec61d36a5c3)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 14 Sep 2023 05:13:42 +0000

```
Closes BATAPI-405

## Description:

* finish implementation

* update record

* wip: implement create report endpoint
```

### Added version information to the list tenants API endpoint, and added UI to display version information in the environments list.

* [view commit 2a20b36](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2a20b364cdb03b258002aa45da57df2b02452c0d)
* Author (Committer): Paul Wheeler (Dale O'Neill)
* Date: Wed, 13 Sep 2023 00:11:11 +0000

```
Closes BATAPI-424
Closes BATAPI-398

## Description:

* Fixed version check json path expressions in test service-configs

* BATAPI-424: implemented support for including version information in the response from the tenants controller.

* Added Version controller actions to the postman collection.

 * Fixed issue with HttpResponseBodyVersionCheckDefinition serialization
 * Added version check configuration and support to http-metric-test-app

* Display version information for each root service

* Added version information to the list tenants API endpoint.
```

### Version Info Caching

* [view commit 97f63a7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/97f63a7d2e35929de759c1f03f1ee97d7114037c)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 11 Sep 2023 20:11:35 +0000

```
Closes BATAPI-415

## Description:

* formatting

* add caching to version controller
```

### Update onClose methods to re-init namespace and confimap watchers on close

* [view commit 5ac0472](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5ac0472eadf10f6014d8877e9c1f28d2ba673a23)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 7 Sep 2023 13:47:12 -0700



### Updates generated SONAR API client with latest changes: * Get user permission tree endpoints * Post/Get service version endpoints

* [view commit 00fd0ce](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/00fd0ceb76d5280f1ea473324f6641b58bbf1467)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 8 Sep 2023 02:01:24 +0000



### create new db entity for error information

* [view commit e469bfd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e469bfdf1742a7234c917c34cfafef3184a4b3e2)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 8 Sep 2023 01:38:14 +0000

```
Closes BATAPI-390

## Description:

* create new db entity for error information

Closes BATAPI-390
```

### Fixed version check json path expressions in test service-configs

* [view commit e33976a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e33976a064b3d9c930e75737eac8ebbcc311c86e)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 7 Sep 2023 14:58:58 -1000



### Added Version controller actions to the postman collection.

* [view commit a0f8485](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a0f8485f9c2a43039f15d13e095978150465e5d2)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 7 Sep 2023 14:28:39 -1000

```
 * Fixed issue with HttpResponseBodyVersionCheckDefinition serialization
 * Added version check configuration and support to http-metric-test-app
```

### Make it possible to deliver dev build container images to artifactory for testing without deploying to our manifest repo.

* [view commit 52df1a1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/52df1a16e1e8b754566a66b76a274030651e0153)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 7 Sep 2023 10:54:14 -1000



### Merge branch 'release-0.3.1'

* [view commit 060da45](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/060da454034a4572a56f2ca8beb37798bb66e681)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 7 Sep 2023 09:23:56 -1000



### implement service version detail module with static data

* [view commit 71eba92](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/71eba9257526b334ba2daa4615c23455ea66dc59)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 6 Sep 2023 18:32:56 +0000

```
Closes BATAPI-397
```

### Update status cache with a single upsert instead of a transactional read and update.

* [view commit dde6b70](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dde6b70d8325c294d74a06d7f414c1477cea179c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Sat, 2 Sep 2023 22:54:01 -1000



### Fixed the performance of the API key validation cache.

* [view commit 9b5d9a1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9b5d9a1561f31d4454ecb4becbdc8afd167c08d1)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 25 Aug 2023 23:43:23 -1000

```
 * Only one task run per API key
 * Limited the number of concurrent API key validations
```

### BATAPI-392 HTTP version check configuration model

* [view commit d846970](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d846970839cfcfa740620fc7ba29976c5d0dc482)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 1 Sep 2023 20:37:05 +0000

```
* Adds configuration model for HTTP response body version checks
```

### Resolve BATAPI-388 "Version controller"

* [view commit 0aefac7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0aefac733c39f90ca0ad22f38d8a5169ec51061b)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 1 Sep 2023 20:01:11 +0000

```
Closes BATAPI-388

## Description:

* Implemented version controller with two endpoints, RecordServiceVersion and GetSpecificServiceVersionDetails

Closes BATAPI-388
```

### Add url, description, and update service name to use display name data

* [view commit 6704f3a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6704f3a211047cb62f088ba79c827a1f2cda23b7)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 30 Aug 2023 00:48:30 -0700



### BATAPI-380 New endpoints return service status in body (XML and Json)

* [view commit d1a2049](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d1a2049b707246b8742db558eb389df2eb1a0fdc)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Fri, 1 Sep 2023 04:46:55 +0000

```
Closes BATAPI-380
```

### BATAPI-379 Add Http body conditions for XML and Json

* [view commit fa562ec](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fa562ec43e954de0113ea86816691bb3920af005)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 30 Aug 2023 18:41:30 +0000

```
Closes BATAPI-379
* Added HttpJsonTest and HttpXmlTest Health Checks to service-Config.json
```

### Merge branch 'main' of https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar into feature-batapi-401-http-health-check-url-display

* [view commit 92b1efa](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/92b1efa98597e076144d69e087899adb45b40dbd)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 29 Aug 2023 13:43:05 -0600



### Adds optional url property to IHealthCheckDefinition union type and makes duration and expression properties optional as well Adds link to health check uri for http type health checks

* [view commit a8a1e9a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a8a1e9aa198f05a2baed1a3ca864a38a8cb87125)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 29 Aug 2023 12:41:37 -0600



### Makes rendering of health check description conditional on whether description exists

* [view commit 6c1254c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6c1254c51b13604e44b7af0c0f0c7c7661b67960)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 29 Aug 2023 12:45:54 -0600



### SONAR Agent Helm Chart Release v0.3.0

* [view commit 0139a09](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0139a099daea5cd650b8870d26a2cd870af1e906)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Mon, 28 Aug 2023 15:27:43 -1000



### Moves health check description to top drawer section from conditions section

* [view commit cd4ddf8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cd4ddf80092b2d22d7cfa4bab292bc0522e88da1)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 29 Aug 2023 12:41:12 -0600



### Adds external link svg icon

* [view commit 0186069](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/018606937d6f0c4be0674f72943dec8d63d80dd5)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 29 Aug 2023 12:37:42 -0600



### implement configuration for version checks

* [view commit 6edad8c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6edad8cad96cad08e23e316f66be19374b909ce1)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 29 Aug 2023 02:03:05 +0000

```
Closes BATAPI-\{ISSUE_ID}

## Description:

* implement configuration for version checks
```

### wip: another perf hotfix.

* [view commit ae6d7b9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ae6d7b9bafe404b1e37c4c25bb9ebb91614e1dde)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 25 Aug 2023 23:43:23 -1000



### Implement data smoothing to sonar-agent's health check evaluation

* [view commit 3cdad40](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3cdad40af82a00fd89e476579f4de2d7c06abfc6)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 25 Aug 2023 20:35:58 +0000

```
Closes BATAPI-381

## Description:

* initial implementation
```

### Add default value for ApiKeyId such that agent will use new apikey format Update environment name for double underscores Remove apiKey from base appsettings.k8s.json as apiKey is loaded from k8 secrets

* [view commit 7043ba7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7043ba7704cd0f168cae1f72e33c036284c15fee)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 22 Aug 2023 12:05:04 -0700



### Add ApiKeyId to list of env variables. If values are not stated do not add to env

* [view commit 0ab6dec](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0ab6deca84f297ce7fb70ca6c73c7bdf6de03180)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 21 Aug 2023 17:31:11 -0700


## 0.3.1

### Fixed issue with test database configuration not being applied everywhere.

* [view commit 3ee039c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3ee039c4cfab2aae3eb7b399936282d862f62779)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 16 Aug 2023 21:51:31 -0500



### Fixed build issue creating DbApiKeyRepository.

* [view commit 66b3c2e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/66b3c2ea77c5f28b6afa1fd7721685551404f217)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 16 Aug 2023 13:45:14 -0500



### Update the LastUsage timestamp of ApiKeys without a transaction to avoid conflicts when multiple API requests are handled simultaneously.

* [view commit cc5ac6b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cc5ac6b4f876307e74c7575ffb31fff171600e57)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 11 Aug 2023 14:44:27 -0500



### Merge branch 'release-0.2.1' into 'main'

* [view commit 3084d7c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3084d7c74a2aee6cbe8d2d00c9f1719f85c36fb8)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 16 Aug 2023 17:25:49 +0000

```
HOTFIX: Cache ApiKey->KeyId lookup so that performance is less horrible

See merge request ado-repositories/oit/waynetech/sonar!204
```

### HOTFIX: further reduce CPU utilization validating new API keys for the first time, log cache misses.

* [view commit 6fbe52f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6fbe52f62be272c8fd3e76442b887e690a550732)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 15 Aug 2023 23:39:34 -0500



### Added RequestTracingMiddleware

* [view commit 5ee6459](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5ee6459dfd7981b22883f762e9d6eb71a874ea51)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 15 Aug 2023 23:36:30 -0500



### HOTFIX: Cache ApiKey->KeyId lookup so that performance is less horrible.

* [view commit b4c116e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b4c116e50c8a09386dcb55e4ba5cfc150459ac0d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 15 Aug 2023 18:39:21 -0500



### BATAPI-369 Removed Prometheus client code in favor of the PrometheusSDK nuget package

* [view commit 1489049](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/14890496c24ebd0e299e9790276610e119d609a7)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 14 Aug 2023 16:31:34 +0000

```
Closes BATAPI-369

* Removed Prometheus client code in favor of the PrometheusSDK nuget package created by intern Ridge working with Blaise.The nuget package code is from the sonar Prometheus client code.
```

### BATAPI-363 User Permission Unique Constraints

* [view commit 7406c1f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7406c1f118fe1058e6b6e7e7c188f330c440fb89)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 11 Aug 2023 16:48:39 +0000

```
* Adds unique partial indexes for user_permission table to ensure created user permissions are unique in each of our three scoping scenarios (globally scoped, environment scoped, tenant scoped)
* Adds ResourceAlreadyExistsException type and exception handling to translate DB unique constraint violations into 409 HTTP response
```

### add offline_access scope to enable refresh token behavior

* [view commit bc66fc4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bc66fc4bfe1676cc39b8b34b9c82d93180674622)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 9 Aug 2023 19:42:41 +0000

```
Closes BATAPI-348

## Description:

* add offline_access scope to enable refresh token behavior
```

### BATAPI-362 Remove duplicate roles listed in Create API Key dialog

* [view commit 736e26c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/736e26c832684ecc567a8f430de74f04de79587f)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 9 Aug 2023 17:40:12 +0000

```
Closes BATAPI-362

* Removed permissionsOptions from options property in Create API Keys dialog. Set the options property to {roles} which is a static list of sonar Roles.  This same options is used when adding user permissions.
```

## 0.3.0

### BATAPI-311 Sonar Agent Helm Chart Pipeline

* [view commit 4826bea](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4826bea9b05f02fa257ea1061cd3a5184edac994)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 7 Aug 2023 16:50:37 +0000

```
* Adds a new pipeline that lints and delivers the Sonar Agent Helm chart
* Lint runs on every pipeline run, deliver only runs on `chart-M.m.p` git tags
```

### BATAPI-377 Updated ApiKey to include date created and last usage of the Key.

* [view commit cb96315](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cb963156e886ac21eeccf23de417ff344b0a5ba1)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Fri, 4 Aug 2023 03:27:49 +0000

```
Closes BATAPI-337

* Added ApiKey creation and usage date time.  Whenever a key is used/authenticated the apiKey record has its last usage field updated with the current DateTime.

* Database migration

* Key management UI displays these added new fields.
```

### Resolve BATAPI-361 "Protected route component"

* [view commit 979c349](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/979c349d8f8876b6fbc43b65e6418e0a602434ae)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 3 Aug 2023 17:55:12 +0000

```
Closes BATAPI-361

## Description:
* Add protected route component
```

### ## Description:

* [view commit 2afa300](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2afa300375b04ca8a538de81edfe620fd83161b1)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 2 Aug 2023 17:44:05 +0000

```
* Fix broken dependency reference in deployment pipeline trigger job (related to BATAPI-309)
```

### Add markdown and images in docs folder, resolve broken links, updated collection to support JWT auth, added pre-request scripts to health-check-data and prometheus collections

* [view commit addcfe9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/addcfe91eb3cc09cb300b929425edd12a615933d)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 28 Jul 2023 16:35:06 -0700



### Update postman collection

* [view commit b725a70](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b725a708ff91d7151fcc422f3b654b7e04649c14)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 28 Jul 2023 14:34:19 -0700



### BATAPI-309: remove agent deployment jobs from SONAR CI/CD deployment pipeline

* [view commit 41fb3d4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/41fb3d42ec20241df7b6153e604ec8cfeb02d421)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 1 Aug 2023 21:57:20 +0000

```
## Description:
Part 1 of BATAPI-309

* Remove all deployment jobs for the Agent from the Sonar CI/CD deployment pipeline (both the lower and upper realm deployments) in [deployment-pipeline.yml](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/blob/main/.gitlab-ci/deployment-pipeline.yml).
```

### BATAPI-349 Created a user Permissions controller test

* [view commit 9a8e40a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9a8e40aa22fdc3ea348b60a2be1f930e8b62ec30)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 1 Aug 2023 20:52:03 +0000

```
Closes BATAPI-349

* Implements tests for all endpoints for user permission controller.

* Made changes ApiControllerTestsBase and ApiIntegrationTestFixture so default behavior keeps database changes for every test run. Added option so database can be recreated on each run.

* Added Policy AllowAnyScope to GetPermissions and GetCurrentUser in the UserPermission controller. Also added new method to the UserPermission Repository used when using API Key for authorization.
```

### Fix Sonar API README error

* [view commit 329169b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/329169b60bc39d4396b0f82d79f6f6f99c5b7cee)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 1 Aug 2023 17:06:49 +0000

```
Adds missing reference to dotnet ef tool in CLI command example
```

### Feature 335 add permission dialog

* [view commit 6180be5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6180be5ed4878437c8aa9dc2f5a3115e20c4e59b)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 1 Aug 2023 00:58:00 +0000

```
Closes BATAPI-335

## Description:

* Implements user permission addition
* Some refactoring for hooks used by multiple components
* Some minor restyling of modals for readability
```

### Closes BATAPI-334

* [view commit 2b47ccd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2b47ccdfe7cfdc8bc68aa7804f843b4616a53012)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 28 Jul 2023 02:46:56 +0000

```
* Implements user permission deletion.
    * Does not let a user delete their own permissions.
* Implements sorting on the user permissions table.
* Some minor styles refactoring.
* Handle scenario when no data is available to the current user.
```

### Resolve BATAPI-359 "React query refactor"

* [view commit a8ca56c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a8ca56c55949081c444ad5ed0ecd5cc4b32118cd)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 27 Jul 2023 18:01:48 +0000

```
Closes BATAPI-359

## Description:

* implement refactor

* resolve merge conflicts

* configure global options for queryClient

* implement POC in CreateKeyForm.tsx
```

### Fixes unused import error

* [view commit 7fd9ca7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7fd9ca7d47ac5ebd4de6cfe0857925da49652b0a)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 26 Jul 2023 13:21:09 -0600



### Refactor Sonar UI Global State

* [view commit bca988c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bca988c496e218ed3fa0eea95a91e99836c23763)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 26 Jul 2023 19:17:19 +0000

```
* Drop the `SonarApiProvider`, and replace with a more generic `AppContextProvider`.
* This new provider provides Sonar API client and the RQ client in the same way as the `SonarApiProvider` did.
* Additionally, this provider adds user context with info about the currently logged-in user, login, and logout callbacks.
* Refactors the login and logout UI components to use the user context and it's callbacks.
* I first tried adding the user info context with a separate use-effect hook, but that wound up being pretty inefficient, and since the sonarApi client and the userInfo are ultimately dependent on the Okta state, it worked better to handle all those concerns together in one `useEffect`.
```

### fix bug with open api spec for POST method

* [view commit fda3a86](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fda3a86874dc1b6a3a44854bda1f25ba9d644d11)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 26 Jul 2023 18:30:51 +0000

```
Closes BATAPI-351

## Description:

* fix bug with open api spec for POST method
```

### BATAPI-334 User Permissions Management UI: PART 2 - Live Data Retrieval

* [view commit 2541dac](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2541dac7fd87f07bf7d894006e2cce84de3031d5)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 26 Jul 2023 14:53:55 +0000

```
* Implements loading real data from the Sonar backend for the user management UI.
* Uses our new pattern for separating data concerns into their own hooks module.
* Gets rid of the test data junk.
```

### Address wrong instantiation

* [view commit 7248229](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7248229bedb749f2765fbe276786b9ba9d9e4dae)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 25 Jul 2023 11:00:53 -0700



### Update queries to use Sonar API provider

* [view commit 64d6a86](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/64d6a86179c6ee567ac100418b1792bf63ca6b15)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 24 Jul 2023 22:58:46 -0700



### Updates generated Typescript Sonar API client with recent User Permissions API additions

* [view commit 4a1c8fb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4a1c8fb974bf232f6be12364c372d52e8a0d0640)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 25 Jul 2023 16:18:34 +0000



### * Always use secure: true request parameter * Adds a queryClient and sonarApi provider component and hook, and an example of how its used

* [view commit 5060e72](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5060e72a5e66a3f20868802e351becc703c92fa9)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 24 Jul 2023 22:44:38 +0000



### finish create env implementation

* [view commit 9bf6bb0](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9bf6bb002fb9caafb00902f704f8470ba7e705d3)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Mon, 24 Jul 2023 22:20:40 +0000

```
Closes BATAPI-336

## Description:

* finish create env implementation
* refactor dialog into shared component, refactor api keys to use shared component

Closes BATAPI-336
```

### Update alert text

* [view commit ed1eaa6](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ed1eaa6b966516436868045eb410ba0b1cf4f994)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 21 Jul 2023 17:35:41 -0700



### Update query to useMutation

* [view commit 80726b8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/80726b869f16ae4a10fd08bd173cc5b8d1492d89)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 21 Jul 2023 13:17:34 -0700



### Create modal, and delete ability

* [view commit fca0c54](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fca0c544a56f52d3d816217b329614c774be3f45)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 20 Jul 2023 13:10:38 -0700



### Batapi permission controller

* [view commit dc30424](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dc30424de54e6052fdb3d10b5505e4a1f6e3f84b)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Mon, 24 Jul 2023 17:24:13 +0000

```
Closes BATAPI-316

## Description:

* Add API Controller to support the management of User Permissions

```

### BATAPI-334 User Permissions Management UI: PART 1 - Frontend implementation with fake data

* [view commit cc02138](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cc0213833371a048ad986068d8b657d9540b6bb1)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 21 Jul 2023 21:46:34 +0000

```
This PR is only a partial implementation of BATAPI-334. I want to get what I've currently got finished in the UI merged into main in order to unblock some of Teresa's work on BATAPI-335. We'll both be dependent on BATAPI-316 for talking to the backend after that is merged (in review now), but this will at least unblock the frontend parts of 335. I will be following up with the completion of the 334 in a second PR.

Included in this PR:
* Implemented (most of) the main frontend functionality for user permissions management using **test data**.
* Did some refactoring around shared components and styles that touched the ApiKeys work that has already been merged.

Not in this PR that will be included in the next one:
* Data interactions with the backend API.
* The "No Data" variations on the two data tables.
* Sorting buttons on column headers in user permissions table (parts of this will be dependent on backend implementation).
* Disabling deletion of one's own permissions (currently the user permissions table shows the delete button on all rows, but the UI mockup requires that you cannot delete your own permissions).
```

### quickfix for tenant dropdown bug

* [view commit a497864](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a497864c7de1fb7f96f3cd7b3214a4dcb10614b2)
* Author (Committer): btakushi (btakushi)
* Date: Thu, 20 Jul 2023 14:22:22 -1000



### * Fix linter errors * Fix visual alignment issues of button content

* [view commit 51794b1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/51794b1deeffbe42865ca417e7d5b8365bb878bb)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 20 Jul 2023 21:37:54 +0000



### Sonar UI GhostActionButton

* [view commit 621a68f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/621a68f0f14f332c6ab23f023ec240606c3eeefd)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 20 Jul 2023 20:01:41 +0000

```
* Adds GhostActionButton shared component
* Refactors previously copy-pasted buttons to use shared component
```

### Resolve BATAPI-315 "Create key ui"

* [view commit d698294](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d6982947b16274925ecbf55ace1eada1eedf381c)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 19 Jul 2023 21:35:53 +0000

```
Closes BATAPI-315

## Description:

* Implement Create API Key feature

Closes BATAPI-315
```

### Adds shaded accent color to themes

* [view commit 37945b4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/37945b422f55934e576d5be07e995099fdd22882)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 19 Jul 2023 11:15:12 -0600



### Fixes merge issue

* [view commit 025c06f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/025c06f196111706b8b084f601cfe850dbd24622)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 19 Jul 2023 10:53:30 -0600



### Enabled cascading deletes for ApiKeys and UserPermissions. Made behavior explicit for other entities.

* [view commit f1b229e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f1b229e4d6bd9dd58515721c43fdc0ed207e7ec8)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 18 Jul 2023 18:28:34 -1000



### sonar-api: added DeleteEnvironment action.

* [view commit 2365545](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/23655453d5d9072da3c4f3c4704fa681f5262e14)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 14 Jul 2023 12:25:22 -1000



### Updated dockerfiles to remove busybox

* [view commit 5b4d739](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5b4d739ba0461aa5805ccf726e12dda86a5a474c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 18 Jul 2023 14:39:45 -1000



### sonar-ui: Added license headers for our SVG icons.

* [view commit 47113f1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/47113f188f1d0e334a6b32d89df12739cf29daf2)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 18 Jul 2023 13:59:26 -1000



### Prefixes Okta config override env var names with REACT_APP_ so they get embedded in the app bundle at build time; updates developer doco with instructions on how to run the UI pointed at a custom Okta instance

* [view commit 0da72c8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/0da72c87e3e2ca607a1d920c2fdbff5db5ca86ea)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Tue, 18 Jul 2023 16:56:27 -0600



### Upgrade Okta.AspNetCore to v4

* [view commit 56cc582](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/56cc582939926fb9024159a6e72746af2493682a)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 18 Jul 2023 10:00:21 -1000



### Updated Okta configuration to use IDM-Impl: SONAR Dev application

* [view commit 6c5df77](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6c5df77b249ac9e03faf6cb92e2141ac95485b41)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Sat, 15 Jul 2023 12:29:50 -1000

```
Resolved issues with IDM Okta integration

 * Use id_token not access_token for accessing the SONAR API
 * Expect the email address to be in the email claim, not the subject
   claim
 * Switched to using "name" from IDM instead of firstName/lastName
 * Updated SSO data migration to replace FirstName/LastName with
   FullName
```

### Added support for configuring okta settings via appsettings

* [view commit 1545620](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/15456203f9e3c5a40b2c7de56f7efe6f4b3afce4)
* Author (Committer): Teresa Tran (Paul Wheeler)
* Date: Tue, 18 Jul 2023 08:40:34 +0000

```
Closes BATAPI-331
```

### Resolve BATAPI-313 "Api key list view"

* [view commit 53b8702](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/53b8702aa861b64a7174ba8befd8665acefef0de)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 18 Jul 2023 03:27:48 +0000

```
Closes BATAPI-313

## Description:

* Implemented table with test data, pagination

Closes BATAPI-313
```

### * Mocks config module in App.test

* [view commit f3cd73a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f3cd73a35706bbbae1ef86f4f558e2d18e19dde7)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 17 Jul 2023 23:42:15 +0000



### sonar-ui: User Upsert support

* [view commit e31e195](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e31e1957da6f9881c222efadca85774363db4782)
* Author (Committer): Stephen Brey (Paul Wheeler)
* Date: Fri, 14 Jul 2023 23:40:15 +0000

```
Closes BATAPI-307

* Adds config module; refactors Okta configuration; moves router context to outer scope.
* Fixes the Okta domain in the API backend.
* Ensures the user is ultimately sent back to their starting point after they complete the login flow.
* Regenerates the API and client and DTOs to include latest User stuff.
* Updates/inserts user records in Sonar API when user logs in.
```

### Added SSO support to sonar UI

* [view commit 602ddd4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/602ddd405933a468fca164edf1ad39b207f8e919)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 13 Jul 2023 19:17:09 +0000

```
* Create modal dropdown, add svg icons, load Okta issuer and IDclient from file, attach OktaAuth to login button
* create use query wrapper for attaching okta JWT credentials
* update k8s configmap for sonar-ui settings

Author: Kevin Ly <kevin.ly@cms.hhs.gov>

Closes BATAPI-303
```

### Merge branch 'main' of https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar into feature-sso

* [view commit fed3b08](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fed3b08d6140062a6f6818beab16836e18766552)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 13 Jul 2023 12:24:56 -0600



### Increases tolerance of floating-point comparison of timestamps to 1ms from 0.5ms

* [view commit 36fef43](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/36fef438416a211298361faa6227846bc16eb2be)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 13 Jul 2023 11:30:16 -0600



### BATAPI-306 User Controller

* [view commit cc91b50](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cc91b50268ca8a7c6fdd450996f0f9049bdaaa84)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 13 Jul 2023 03:48:19 +0000

```
Closes BATAPI-306

## Description:

* implement user controller

Closes BATAPI-306
```

### Merge branch 'main' of https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar into feature-sso

* [view commit fb7bdae](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fb7bdae5c9aa95be79c596ae1cac307db3fad199)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 10 Jul 2023 10:29:56 -0600



### Implemented Okta JwtBearer support in SONAR Api.

* [view commit acf9e93](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/acf9e93287606c79f5496364ce288cee262ba461)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 5 Jul 2023 23:15:42 -1000

```
Closes BATAPI-305
```

### BATAPI-308, BATAPI-304: Adds database migrations for adding the SSO user and user_permission tables.

* [view commit 2eeee33](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2eeee336bf21f4f2fe6eabfd681014b2263b2b19)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 7 Jul 2023 16:30:47 +0000



### Added UserPermission data entity.

* [view commit ac6d5e2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ac6d5e22a5854ca5061181e4677114547dda3c5a)
* Author (Committer): Paul Wheeler (Stephen Brey)
* Date: Sun, 2 Jul 2023 23:20:03 -1000



### Fixes merge conflicts

* [view commit 35cbcb8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/35cbcb814e03c1011dac197be9310131ae79b00e)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 6 Jul 2023 10:40:33 -0600



### Changed table name to be all lower case and when creating the table just model the entity with no additional updates.

* [view commit 55447cf](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/55447cf71ad6e1c2ef042ac549123ba2701004c7)
* Author (Committer): Dale O'Neill (Stephen Brey)
* Date: Fri, 30 Jun 2023 18:04:50 -0700



### Added Entity User to the database.

* [view commit 785d806](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/785d806eb6239c7ea289e18a1af817e844ef3c6b)
* Author (Committer): Dale O'Neill (Stephen Brey)
* Date: Thu, 29 Jun 2023 12:57:18 -0700


## 0.2.1

### BATAPI-325 Migration support conversion post-work

* [view commit ad59cc7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ad59cc7bce648cd81fb04039f61d2d969abefa38)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Wed, 12 Jul 2023 19:16:43 +0000

```
Cleans up the code that existed to support the conversion to migration support, but is no longer needed since all existing environments have been converted. Also adds support for targeting a specific database migration.
```

## 0.2.0

### BATAPI-332 Fix migration init container failure

* [view commit 5f3b4d2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5f3b4d2284ce690d7622195c0f69628caadc526a)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Mon, 10 Jul 2023 15:53:43 +0000

```
* Removes schema qualifier from raw SQL text to defer to default schema search path in the PG server
```

### Made some minor tweaks to the sonar ui

* [view commit bf977ad](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bf977ad84f11bf2a63313362ec9c1c7ae4be1713)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 6 Jul 2023 22:58:43 -1000

```
 * Changed the toggle theme label font and vertical alignment
 * Fixed an issue where changing border size on hover changed control
   position
 * Added the ability to over ride the API_URL with an environment
   variable on developer machines
```

### Added support for specifying a Description for a service in the legacy (v1) endpoint.

* [view commit 92b4efc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/92b4efc690ae1643c2b18b5e4aa6f0ff1b0ec459)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Thu, 6 Jul 2023 22:59:31 -1000



### BATAPI-216: EntityFramework Migration Support

* [view commit 31d4d33](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/31d4d332604190b97e31cbecd3f44209fb6feb67)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 6 Jul 2023 16:14:35 +0000

```
* Adds support for EntityFramework migrations, and creates initial database migration.
* Removes the admin API endpoint for initializing the database; now that we have a process for database migration, it's not safe to keep around.
* Adds a new `migratedb` command for migrating the database on the command line.
    * This command also provisions the migration history table for pre-migration-support databases.
* For now I've left a couple pieces of testing code present that are helpful for validation (the old init command is still there, and a small time delay added to the migration method to help with validating concurrent initializers); these need to be removed after this new migration mechanism has been proved-out in the lower environments; will be removed in BATAPI-325.
```

### add ability to specify appSettings location for sonar-api

* [view commit ddbd9cc](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ddbd9cc9bf4f4f6bf475dc7ea1326b6c1abefb53)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Wed, 5 Jul 2023 21:47:15 +0000

```
Closes BATAPI-288

## Description:

* add ability to specify appSettings location for sonar-api
```

### Sonar Agent: Fix failing unit tests

* [view commit 5a50ead](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5a50eada164f1dda16ec60c68a388e901126be0a)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 5 Jul 2023 20:05:25 +0000



### Fix issues that arise when there are existing tenants in the "sonar environment"

* [view commit 2d19d8e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2d19d8e1c8be972092cedd897817c8460dfc96bb)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 30 Jun 2023 23:04:59 -1000

```
The sonar environment is the pre-configured environment where sonar
reports it's self-health-check status.

When there are existing tenants in the sonar environment, merge the
self healh check tenant status with the normal tenants in the response
from the environments and tenants controllers.
```

## 0.1.2

### update service overview to display aggregate status for each child service

* [view commit c5298d5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/c5298d566268e30c83a4dc77d7a4409234fc9ce8)
* Author (Committer): Blaise Takushi (Paul Wheeler)
* Date: Fri, 30 Jun 2023 19:49:30 +0000

```
Closes BATAPI-300
```

### Added aria-label attribute to provide the element with its accessible name.

* [view commit af9b40d](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/af9b40d59e5361bb8a22dabb277ddc3c078d7ac3)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Wed, 28 Jun 2023 14:22:55 -0700



### Additional null checks in UI

* [view commit 1c9ab5e](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1c9ab5eb7ea008601ccd7eefb94ff4804cceea97)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 28 Jun 2023 11:15:36 -0700



### Address PR comments

* [view commit 31c9c5a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/31c9c5a5338f0362654b00b0435bba57efd91ca3)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 28 Jun 2023 10:32:38 -0700



### Create and add sonar-local environment, create new Internal HealthCheck definition, update and add healthModels and configurations

* [view commit 776704b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/776704b85cb70953fd161d3df29c4237ee739a66)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 22 Jun 2023 14:16:53 -0700



### WIP Add sonar to tenants, and service configuration controllers

* [view commit 48819bb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/48819bb387c55deca09c15fb54565cc1987d9791)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 21 Jun 2023 21:45:10 -0700



### Added support for running cypress e2e tests in headless mode.

* [view commit 1f38a9f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1f38a9f7e2ef5e8d0c5833c56cb416cedaa151b5)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 21 Jun 2023 16:00:10 -1000



### Made some tweaks to the cypress tests.

* [view commit 818ac2f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/818ac2f1e4610d1c7832df872156e26bf4b7c8fb)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 21 Jun 2023 15:49:46 -1000

```
 * Use a unique tenant name to avoid conflicts
 * Match the tenant name when opening a service in case other tenants
   with that service name exist
 * Removed unused helper files and functions
 * Made some tweaks so that TypeScript validation in the IDE works
```

### BATAPI-292: Fixed service path parsing (for root service names containing "services") in SONAR UI.

* [view commit 67b6914](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/67b6914c38813f917073cfa9d3d76f249418b00b)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 27 Jun 2023 19:31:16 +0000


## 0.1.1

### SONAR Release v0.1.1

* [view commit 387e74b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/387e74b075f88d0bda7ceb1d1316a7e08197bd7e)
* Author (Committer): Stephen Brey (Paul Wheeler)
* Date: Fri, 23 Jun 2023 12:47:41 -0600

### Update text to "Toggle Theme"

* [view commit 4bc630c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/4bc630cfe29e48b679b82f657b8e7cc2cc50af4b)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Fri, 23 Jun 2023 15:28:01 -0700



### Added text to label which gives a larger clickable area.  This meets section 508 requirements

* [view commit 835c792](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/835c792dff52396435756b4c7772018f8aac3125)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 22 Jun 2023 20:46:47 -0700



### fixed date logic for timestamp hover

* [view commit ab8ffa1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ab8ffa1ceae6e63b16b62f8ec67378d18f6d114c)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Fri, 23 Jun 2023 20:38:36 +0000

```
Closes BATAPI-291

## Description:

* fixed date logic for timestamp hover
```

### BATAPI-278: fixed lint issue

* [view commit f2544f1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f2544f187067dfd3a60dc9c18cfc68c4531c436d)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Fri, 23 Jun 2023 12:17:32 -0700



### BATAPI-278: removed unnecessary pg-related devDependency/modules/configuration and white spaces

* [view commit d7818b8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d7818b8deae6ecc4f23444895187634ce399c15a)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Fri, 23 Jun 2023 11:15:47 -0700



### BATAPI-278: added assertion for element that should not exist

* [view commit ca39397](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ca3939787659dda10141a2a7d1496ee37bf7b9c9)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 21 Jun 2023 13:24:21 -0700



### BATAPI-278: added cleanup for e2e test

* [view commit cf95a46](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/cf95a46ef32cfa87e3d494d884d9486fd5f9e8ab)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 21 Jun 2023 13:10:50 -0700



### BATAPI-278: updated sonar-ui README with cypress section

* [view commit 81b7f88](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/81b7f889ed4d11e1fca1cf51ddd08165253b79e1)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 21 Jun 2023 01:44:02 -0700



### BATAPI-278: root service-only service hierarchy config e2e test

* [view commit ab95b73](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ab95b73d3c204fc279000612bdb12db5771ea9bb)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Wed, 21 Jun 2023 01:22:49 -0700



### BATAPI-278: connect to database, test UI navigation bar

* [view commit 9cbd8bd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9cbd8bddd837c34bd31916b90950ea35c5a2fc5f)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 20 Jun 2023 22:36:52 -0700


### Closes BATAPI-279 - Pipeline Refactor

* [view commit ce5143c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/ce5143ca39507f3c1de43ce11c824d7475000ebb)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Fri, 23 Jun 2023 18:09:52 +0000

```
Converts Sonar's GitLab CI/CD pipeline configuration to a parent-child architecture, and leverages the shared pipeline triggers (see https://code.batcave.internal.cms.gov/devops-pipelines/pipeline-triggers).

The root .gitlab-ci.yml is the parent pipeline, which triggers three child pipelines:
- .gitlab-ci/frontend-build-pipeline.yml: Handles build and image delivery of the Sonar UI.
- .gitlab-ci/backend-build-pipeline.yml: Handles build and image delivery of the Sonar backend (both API and Agent).
- .gitlab-ci/deployment-pipeline.yml: Handles serial deployment of all Sonar components.

Feature/bugfix branch pipelines only run build, lint, and test jobs; they don't push any artifacts.

The main branch runs the above jobs, and additionally runs the SAST, deliver, and deploy jobs (which push build artifacts and deployment triggers).

There's two pipeline flavors for the main branch, `dev` and `release`; they both run the same set of jobs, but under different conditions using different artifact tags:
- Dev pipelines run for non-tagged commits (i.e. merged feature/bugfix branches), tag all artifacts with the short commit SHA, and automatically deploy to the `dev`, `k3d`, and `impl` environments.
- Release pipelines run for new git semver tags only, tag all artifacts with the version tag, automatically deploy to the `test` environment, and can manually deploy to the `prod` environment via button-click in the GitLab UI.
```

### fix undefined status bug in health status drawer

* [view commit f6db5db](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f6db5dbbd6974f7984f2d66bb75a3c1178a6c79e)
* Author (Committer): btakushi (btakushi)
* Date: Thu, 22 Jun 2023 09:18:25 -1000



### update base image to 1.25-alpine-slim

* [view commit df9a981](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/df9a981a2c1eeff02448da8fefb1dcfd5a3f4431)
* Author (Committer): btakushi (btakushi)
* Date: Tue, 20 Jun 2023 14:58:48 -1000



### add exception handling for OperationCanceled and SocketException

* [view commit 1c917a3](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1c917a368b6a571563efaabfd776d8013cdf18f1)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Thu, 15 Jun 2023 17:58:10 +0000

```
Closes BATAPI-284

## Description:

* add exception handling for OperationCanceled and SocketException
```

### Undefine error

* [view commit a3cdf20](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a3cdf200e7005c99aaa330c9287c9fe8b35974d2)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 14 Jun 2023 20:31:54 -0700



### Add conditional to deployment stages

* [view commit 23c1a72](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/23c1a72939a9a5bd9ad20879f5d6205c41b030f6)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 14 Jun 2023 18:43:14 -0700



### Change to gitlab-ci.yaml

* [view commit 1a156bd](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1a156bd32623595b44fe90851c8a03373cadfc15)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 14 Jun 2023 18:25:18 -0700



### Update nginx configuration and service path

* [view commit 3a38fe9](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3a38fe9c655037bfb800c96101d61b89b90a11ff)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 14 Jun 2023 15:47:26 -0700



### Add additional environments

* [view commit 52c6f6c](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/52c6f6c4a0fb426bbed773e042277985726567d0)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 13 Jun 2023 11:56:34 -0700



### Re-enable existing pipelines

* [view commit 11a294f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/11a294f9512b604771bfaf97630fcdcd5e308ffa)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 12 Jun 2023 17:18:54 -0700



### Address lint

* [view commit 36efc4a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/36efc4af02c6cd9b5b0330bb5602409e2c9b3c73)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Mon, 12 Jun 2023 16:57:45 -0700



### Add deployment to dev

* [view commit 67a7714](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/67a771420293e9ec058978de40f2c1dea4abbb21)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 9 Jun 2023 17:14:56 -0700



### Add build, test and pipeline-trigger stages

* [view commit 81d9d14](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/81d9d141c5761ae798a7a2e0cdc63ee36250cb76)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 8 Jun 2023 11:53:19 -0700



### Made the work factor for ApiKey hashing configurable so the performance impact on tests is less severe.

* [view commit 3ea23ce](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3ea23ced9b3ff72cc1e3c64749cb154a0f03e95d)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 9 Jun 2023 23:56:03 -1000



### refactored authentication and authorization to use ASP.Net conventions.

* [view commit dbf6d14](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dbf6d144cc0743e7c46103ebc7a7fc4071adc31c)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Tue, 9 May 2023 09:10:10 -1000



### BATAPI-254: fixed lint issue in Service

* [view commit 8df8597](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/8df8597a665f6329c80cc507f57ac7d5e9f694f6)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 12 Jun 2023 16:17:28 -0700



### BATAPI-254: made health check in status history drawer unclickable

* [view commit b20e603](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b20e60304c095f5b154760bfaeea3f31b58ac0d4)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Mon, 12 Jun 2023 15:50:06 -0700



### BATAPI-254: added newline to tooltip text

* [view commit 541bf2f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/541bf2fd1bf6d0e9e5f1d2aa859e83fe8a1a03a7)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Fri, 9 Jun 2023 14:38:44 -0700



### BATAPI-254: removed StaticTextFontStyle, made font in overviews the same, updated header font

* [view commit b06869b](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/b06869b00dc984c8f5223d3458cb1a30cce672db)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Fri, 9 Jun 2023 14:14:50 -0700



### BATAPI-254: updated status history tile and drawer style, added hover tooltip, utc timestamps converted to browser timezone

* [view commit 1242d71](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1242d715c950a0fb6d226bcd5fd3831b048aeb95)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Fri, 9 Jun 2023 11:56:01 -0700



### update readme

* [view commit 6fc805f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6fc805f017a66cffc213df872c85c7131afc2fa4)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Thu, 8 Jun 2023 14:43:42 -0700



### Add UI to ingress

* [view commit bbe67fe](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/bbe67fe41fe293760b6cce3683b763ee7ac3bedc)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 7 Jun 2023 20:16:39 -0700



### Update to readme, additional targetPort for UI

* [view commit 1239799](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1239799089db71525aa9ca4d6898f911d8589969)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Wed, 7 Jun 2023 13:49:16 -0700



### Adding sonar-ui to k8 env

* [view commit 1c2f4ed](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/1c2f4ed35ebadb796164c9b5966dbad7f096b1ea)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Tue, 6 Jun 2023 18:45:44 -0700



### Add build Dockerfile, nginx, and update image

* [view commit 40f5e3f](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/40f5e3f10c940542c5631afc96476f30b7a1e606)
* Author (Committer): Kevin Ly (Kevin Ly)
* Date: Fri, 2 Jun 2023 10:26:43 -0700



### Added support for ingress in our local K3D cluster setup so we can access services in the cluster without port forwarding

* [view commit e36cad1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e36cad139bc010cd9a785388518a5e41cfe06ac4)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Wed, 7 Jun 2023 13:42:48 -1000



### BATAPI-255: Align health check status details with mockups

* [view commit 9e1a5f1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/9e1a5f1b65868b65b3abfc905a8617a5f9214947)
* Author (Committer): Stephen Brey (Stephen Brey)
* Date: Thu, 8 Jun 2023 15:42:57 +0000

```
Closes BATAPI-255

## Description:

* Moves health check status details view into drawer.
* Updates styling to match what's in the mockup.
```

### removed a debug setting ApiKey2 back to ApiKey and a minor code format.  All parameters pushed onto a separate line.

* [view commit 5e52448](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/5e524489c3fcc31e818f6558f3233cb161189409)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 20:36:06 -0700



### Removed unused directives.

* [view commit fb9b400](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/fb9b40099de069897f2f5ba17b8d12eca50a9754)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 10:30:06 -0700



### Updated ApiKeyDataHelper to support checking for keys to see if they are valid.

* [view commit dfe1d0a](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/dfe1d0afc2ea96878e69d7a69e5d7e288cf676c9)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 10:06:40 -0700



### Fixed code formatting. Minor changes.

* [view commit a8708f4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/a8708f406ae5d72de86f73d20a42636494c00877)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 09:20:10 -0700



### Do not return key ID except on creation.  Validate any request against the key with encrypted database keys.

* [view commit 3910ae5](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/3910ae54c4cc559e456f9e504cbd29fc8d19bfaf)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 09:15:40 -0700



### BCrypt to key and store in database.

* [view commit 6dc2ddb](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6dc2ddbc99e363e836a599380c214ee43bdff1c0)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Thu, 1 Jun 2023 21:59:47 -0700



### Fixed formatter name so the correct formatter is used.

* [view commit d390241](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/d390241ea0f3da2ec53204f26997bcf8b5e62b36)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 12:53:05 -0700



### Removed commented code - minor change.

* [view commit e05fdc2](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/e05fdc25aaa332c0d4d200164dcc2263668a1375)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 10:42:37 -0700



### Moved Custom Logger from Sona-Agent to sonar core.  Also added cutom formater to sonar-api

* [view commit f536be1](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/f536be1a3e8b45dc75ab57979dcb8fdbf54ea71d)
* Author (Committer): Dale O'Neill (Dale O'Neill)
* Date: Tue, 6 Jun 2023 10:38:41 -0700



### sonar-api: added integration tests for authentication and authorization scenarios.

* [view commit 2b11017](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2b11017465773a8d32db765243495f8ffbf0bf60)
* Author (Committer): Paul Wheeler (Paul Wheeler)
* Date: Fri, 26 May 2023 00:15:31 -1000



### Update .gitlab-ci.yml

* [view commit 6babdd4](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/6babdd46fef6e5a1c8a532387d062973c129319f)
* Author (Committer): Bacchus Jackson (Paul Wheeler)
* Date: Mon, 5 Jun 2023 19:40:32 +0000



### BATAPI-253: Align Dashboard with Mockups - Service View - navigation and container

* [view commit 2f5f187](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2f5f187026ca0f77004e7ed0091d82cba4205393)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Tue, 6 Jun 2023 15:48:50 +0000

```
Closes BATAPI-253

## Description:

- Add breadcrumb navigation to Service view container
- Update Service view to align with mockups format
- When user clicks on a subservice, display that subservice’s info (status history tiles, health checks, and any services) and update breadcrumbs navigation to reflect hierarchy
```

### remove SIAQs from changelog

* [view commit 2691c13](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/2691c13e6a3f71b600a26f9a1e7542986a84537a)
* Author (Committer): btakushi (btakushi)
* Date: Mon, 5 Jun 2023 13:10:46 -1000



### chart-0.1.0

* [view commit 19ba2e7](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/19ba2e7d7161a8893aa0980938bf020ab1453bec)
* Author (Committer): btakushi (btakushi)
* Date: Sun, 4 Jun 2023 20:09:43 -1000


## 0.1.0

### Align Environments page with mockups

* [view commit 65bf8f8](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/65bf8f8daaac7dfb92ec017b042892d0d892d1fe)
* Author (Committer): Blaise Takushi (Blaise Takushi)
* Date: Tue, 23 May 2023 20:20:51 +0000

```
Closes BATAPI-251

## Description:

* Update Environments page to align with mockups, implement expand/close all functionality.

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
```

### Feature 196 fetch health check metric data endpoint

* [view commit 7d1b740](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commit/7d1b740af8ce5afbbe998aa2be2d92c331200eb7)
* Author (Committer): Teresa Tran (Teresa Tran)
* Date: Thu, 27 Apr 2023 01:00:42 +0000

```
Closes BATAPI-196

## Description:

Fetch a single metric time series for a specific health check. The caller should optionally be able to specify a start and end date. The default end date should be the current time in UTC and the default start date should be ten minutes ago in UTC.

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
