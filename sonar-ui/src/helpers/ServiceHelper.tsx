export function getServiceRootStatusAndName(servicePath: string) {
  const serviceList = servicePath.split('/');
  const currentServiceIsRoot = (serviceList.length === 1) ? true : false;
  const serviceName : string = currentServiceIsRoot ?
    servicePath.split('/')[0] : servicePath.split('/').pop()!;

  return { currentServiceIsRoot, serviceName };
}
