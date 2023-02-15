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
