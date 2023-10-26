import React from 'react';
import { IHealthCheckHttpCondition } from "types";

const PathTypeMap: {[key:string]: string} = {
  'HttpBodyJson': 'JsonPath',
  'HttpBodyXml': 'XPath'
};

const statusConditionFragments = (c: IHealthCheckHttpCondition) =>
  [<><b>{c.status}</b>: {c.type} in [{c.statusCodes?.join(', ')}]</>];

const responseTimeConditionFragments = (c: IHealthCheckHttpCondition) =>
  [<><b>{c.status}</b>: {c.type} &gt; {c.responseTime}</>];

const bodyConditionFragments = (c: IHealthCheckHttpCondition) => {
  const pathType = PathTypeMap[c.type ?? ''] ?? 'Path';

  const rv = [<><b>{c.status}</b>: {pathType} "{c.path}" matches Regex "{c.value}"</>]
  if (c.noMatchStatus) {
    rv.push(<><b>{c.noMatchStatus}</b>: {pathType} "{c.path}" doesn't match Regex "{c.value}"</>);
  }

  return rv;
}

export function HttpHealthCheckConditionsList({conditions}: {conditions?: IHealthCheckHttpCondition[]}) {
  const elements = (conditions || []).flatMap((c) => {
    switch (c.type) {
      case 'HttpStatusCode':
        return statusConditionFragments(c);
      case 'HttpResponseTime':
        return responseTimeConditionFragments(c);
      case 'HttpBodyJson':
      case 'HttpBodyXml':
        return bodyConditionFragments(c);
      default:
        return [<>Unknown HTTP condition type {c.type}</>];
    }
  })
  .map((f, i) => (<div key={`httpCondition-${i}`}>{ f }</div>));

  return (<div>{ elements }</div>);
}
