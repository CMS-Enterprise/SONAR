/* eslint-disable */
/* tslint:disable */
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export enum AgentErrorLevel {
  Fatal = "Fatal",
  Error = "Error",
  Warning = "Warning",
}

export enum AgentErrorType {
  Deserialization = "Deserialization",
  Validation = "Validation",
  SaveConfiguration = "SaveConfiguration",
  FetchConfiguration = "FetchConfiguration",
  Execution = "Execution",
  Unknown = "Unknown",
}

export interface AlertReceiverConfiguration {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  type: AlertReceiverType;
  options: AlertReceiverOptions;
}

export type AlertReceiverOptions = object;

export enum AlertReceiverType {
  Email = "Email",
}

export interface AlertSilenceDetails {
  /** @minLength 1 */
  name: string;
}

export interface AlertSilenceView {
  /** @format date-time */
  startsAt: string;
  /** @format date-time */
  endsAt: string;
  /** @minLength 1 */
  silencedBy: string;
}

export interface AlertingConfiguration {
  receivers: AlertReceiverConfiguration[];
}

export interface AlertingRuleConfiguration {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  threshold: HealthStatus;
  /** @minLength 1 */
  receiverName: string;
  /** @format int32 */
  delay?: number;
}

export interface ApiKeyConfiguration {
  /** @format uuid */
  id?: string;
  /**
   * @minLength 0
   * @maxLength 44
   */
  apiKey: string;
  apiKeyType: PermissionType;
  environment?: string | null;
  tenant?: string | null;
  /** @format date-time */
  creation?: string;
  /** @format date-time */
  lastUsage?: string;
}

export interface ApiKeyDetails {
  apiKeyType: PermissionType;
  environment?: string | null;
  tenant?: string | null;
}

export interface Assembly {
  definedTypes?: TypeInfo[] | null;
  exportedTypes?: Type[] | null;
  /** @deprecated */
  codeBase?: string | null;
  entryPoint?: MethodInfo;
  fullName?: string | null;
  imageRuntimeVersion?: string | null;
  isDynamic?: boolean;
  location?: string | null;
  reflectionOnly?: boolean;
  isCollectible?: boolean;
  isFullyTrusted?: boolean;
  customAttributes?: CustomAttributeData[] | null;
  /** @deprecated */
  escapedCodeBase?: string | null;
  manifestModule?: Module;
  modules?: Module[] | null;
  /** @deprecated */
  globalAssemblyCache?: boolean;
  /** @format int64 */
  hostContext?: number;
  securityRuleSet?: SecurityRuleSet;
}

export interface BadRequestException {
  targetSite?: MethodBase;
  message?: string | null;
  innerException?: Exception;
  helpLink?: string | null;
  source?: string | null;
  /** @format int32 */
  hResult?: number;
  stackTrace?: string | null;
  status?: HttpStatusCode;
  problemType?: string | null;
  data?: Record<string, any>;
}

export enum CallingConventions {
  Standard = "Standard",
  VarArgs = "VarArgs",
  Any = "Any",
  HasThis = "HasThis",
  ExplicitThis = "ExplicitThis",
}

export interface ConstructorInfo {
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  attributes?: MethodAttributes;
  methodImplementationFlags?: MethodImplAttributes;
  callingConvention?: CallingConventions;
  isAbstract?: boolean;
  isConstructor?: boolean;
  isFinal?: boolean;
  isHideBySig?: boolean;
  isSpecialName?: boolean;
  isStatic?: boolean;
  isVirtual?: boolean;
  isAssembly?: boolean;
  isFamily?: boolean;
  isFamilyAndAssembly?: boolean;
  isFamilyOrAssembly?: boolean;
  isPrivate?: boolean;
  isPublic?: boolean;
  isConstructedGenericMethod?: boolean;
  isGenericMethod?: boolean;
  isGenericMethodDefinition?: boolean;
  containsGenericParameters?: boolean;
  methodHandle?: RuntimeMethodHandle;
  isSecurityCritical?: boolean;
  isSecuritySafeCritical?: boolean;
  isSecurityTransparent?: boolean;
  memberType?: MemberTypes;
}

export interface CurrentUserView {
  fullName?: string | null;
  email?: string | null;
  isAdmin?: boolean;
}

export interface CustomAttributeData {
  attributeType?: Type;
  constructor?: ConstructorInfo;
  constructorArguments?: CustomAttributeTypedArgument[] | null;
  namedArguments?: CustomAttributeNamedArgument[] | null;
}

export interface CustomAttributeNamedArgument {
  memberInfo?: MemberInfo;
  typedValue?: CustomAttributeTypedArgument;
  memberName?: string | null;
  isField?: boolean;
}

export interface CustomAttributeTypedArgument {
  argumentType?: Type;
  value?: any;
}

/**
 * @maxItems 2
 * @minItems 2
 */
export type DateTimeDoubleValueTuple = (string | number)[];

/**
 * @maxItems 2
 * @minItems 2
 */
export type DateTimeHealthStatusValueTuple = (string | HealthStatus)[];

/**
 * @maxItems 2
 * @minItems 2
 */
export type DateTimeServiceVersionTypeInfoIImmutableListValueTuple = (string | ServiceVersionTypeInfo[])[];

export interface EnvironmentHealth {
  /** @minLength 1 */
  environmentName: string;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
  isNonProd?: boolean;
  isInMaintenance?: boolean;
  inMaintenanceTypes?: string | null;
}

export interface EnvironmentModel {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  isNonProd?: boolean;
}

export interface ErrorReportDetails {
  /** @format date-time */
  timestamp: string;
  tenant?: string | null;
  service?: string | null;
  healthCheckName?: string | null;
  level: AgentErrorLevel;
  type: AgentErrorType;
  /** @minLength 1 */
  message: string;
  configuration?: string | null;
  stackTrace?: string | null;
}

export enum EventAttributes {
  None = "None",
  SpecialName = "SpecialName",
  RTSpecialName = "RTSpecialName",
}

export interface EventInfo {
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  memberType?: MemberTypes;
  attributes?: EventAttributes;
  isSpecialName?: boolean;
  addMethod?: MethodInfo;
  removeMethod?: MethodInfo;
  raiseMethod?: MethodInfo;
  isMulticast?: boolean;
  eventHandlerType?: Type;
}

export interface Exception {
  targetSite?: MethodBase;
  message?: string | null;
  data?: Record<string, any>;
  innerException?: Exception;
  helpLink?: string | null;
  source?: string | null;
  /** @format int32 */
  hResult?: number;
  stackTrace?: string | null;
}

export enum FieldAttributes {
  PrivateScope = "PrivateScope",
  Private = "Private",
  FamANDAssem = "FamANDAssem",
  Assembly = "Assembly",
  Family = "Family",
  FamORAssem = "FamORAssem",
  Public = "Public",
  FieldAccessMask = "FieldAccessMask",
  Static = "Static",
  InitOnly = "InitOnly",
  Literal = "Literal",
  NotSerialized = "NotSerialized",
  HasFieldRVA = "HasFieldRVA",
  SpecialName = "SpecialName",
  RTSpecialName = "RTSpecialName",
  HasFieldMarshal = "HasFieldMarshal",
  PinvokeImpl = "PinvokeImpl",
  HasDefault = "HasDefault",
  ReservedMask = "ReservedMask",
}

export interface FieldInfo {
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  memberType?: MemberTypes;
  attributes?: FieldAttributes;
  fieldType?: Type;
  isInitOnly?: boolean;
  isLiteral?: boolean;
  /** @deprecated */
  isNotSerialized?: boolean;
  isPinvokeImpl?: boolean;
  isSpecialName?: boolean;
  isStatic?: boolean;
  isAssembly?: boolean;
  isFamily?: boolean;
  isFamilyAndAssembly?: boolean;
  isFamilyOrAssembly?: boolean;
  isPrivate?: boolean;
  isPublic?: boolean;
  isSecurityCritical?: boolean;
  isSecuritySafeCritical?: boolean;
  isSecurityTransparent?: boolean;
  fieldHandle?: RuntimeFieldHandle;
}

export enum GenericParameterAttributes {
  None = "None",
  Covariant = "Covariant",
  Contravariant = "Contravariant",
  VarianceMask = "VarianceMask",
  ReferenceTypeConstraint = "ReferenceTypeConstraint",
  NotNullableValueTypeConstraint = "NotNullableValueTypeConstraint",
  DefaultConstructorConstraint = "DefaultConstructorConstraint",
  SpecialConstraintMask = "SpecialConstraintMask",
}

export type HealthCheckDefinition = object;

export interface HealthCheckModel {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  description?: string | null;
  type: HealthCheckType;
  definition: HealthCheckDefinition;
  /** @format int32 */
  smoothingTolerance?: number | null;
}

export enum HealthCheckType {
  PrometheusMetric = "PrometheusMetric",
  LokiMetric = "LokiMetric",
  HttpRequest = "HttpRequest",
  Internal = "Internal",
}

export enum HealthStatus {
  Unknown = "Unknown",
  Online = "Online",
  AtRisk = "AtRisk",
  Degraded = "Degraded",
  Offline = "Offline",
  Maintenance = "Maintenance",
}

export enum HttpStatusCode {
  Continue = "Continue",
  SwitchingProtocols = "SwitchingProtocols",
  Processing = "Processing",
  EarlyHints = "EarlyHints",
  OK = "OK",
  Created = "Created",
  Accepted = "Accepted",
  NonAuthoritativeInformation = "NonAuthoritativeInformation",
  NoContent = "NoContent",
  ResetContent = "ResetContent",
  PartialContent = "PartialContent",
  MultiStatus = "MultiStatus",
  AlreadyReported = "AlreadyReported",
  IMUsed = "IMUsed",
  MultipleChoices = "MultipleChoices",
  MovedPermanently = "MovedPermanently",
  Found = "Found",
  SeeOther = "SeeOther",
  NotModified = "NotModified",
  UseProxy = "UseProxy",
  Unused = "Unused",
  TemporaryRedirect = "TemporaryRedirect",
  PermanentRedirect = "PermanentRedirect",
  BadRequest = "BadRequest",
  Unauthorized = "Unauthorized",
  PaymentRequired = "PaymentRequired",
  Forbidden = "Forbidden",
  NotFound = "NotFound",
  MethodNotAllowed = "MethodNotAllowed",
  NotAcceptable = "NotAcceptable",
  ProxyAuthenticationRequired = "ProxyAuthenticationRequired",
  RequestTimeout = "RequestTimeout",
  Conflict = "Conflict",
  Gone = "Gone",
  LengthRequired = "LengthRequired",
  PreconditionFailed = "PreconditionFailed",
  RequestEntityTooLarge = "RequestEntityTooLarge",
  RequestUriTooLong = "RequestUriTooLong",
  UnsupportedMediaType = "UnsupportedMediaType",
  RequestedRangeNotSatisfiable = "RequestedRangeNotSatisfiable",
  ExpectationFailed = "ExpectationFailed",
  MisdirectedRequest = "MisdirectedRequest",
  UnprocessableEntity = "UnprocessableEntity",
  Locked = "Locked",
  FailedDependency = "FailedDependency",
  UpgradeRequired = "UpgradeRequired",
  PreconditionRequired = "PreconditionRequired",
  TooManyRequests = "TooManyRequests",
  RequestHeaderFieldsTooLarge = "RequestHeaderFieldsTooLarge",
  UnavailableForLegalReasons = "UnavailableForLegalReasons",
  InternalServerError = "InternalServerError",
  NotImplemented = "NotImplemented",
  BadGateway = "BadGateway",
  ServiceUnavailable = "ServiceUnavailable",
  GatewayTimeout = "GatewayTimeout",
  HttpVersionNotSupported = "HttpVersionNotSupported",
  VariantAlsoNegotiates = "VariantAlsoNegotiates",
  InsufficientStorage = "InsufficientStorage",
  LoopDetected = "LoopDetected",
  NotExtended = "NotExtended",
  NetworkAuthenticationRequired = "NetworkAuthenticationRequired",
}

export type ICustomAttributeProvider = object;

export type IntPtr = object;

export enum LayoutKind {
  Sequential = "Sequential",
  Explicit = "Explicit",
  Auto = "Auto",
}

export interface MemberInfo {
  memberType?: MemberTypes;
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
}

export enum MemberTypes {
  Constructor = "Constructor",
  Event = "Event",
  Field = "Field",
  Method = "Method",
  Property = "Property",
  TypeInfo = "TypeInfo",
  Custom = "Custom",
  NestedType = "NestedType",
  All = "All",
}

export enum MethodAttributes {
  PrivateScope = "PrivateScope",
  Private = "Private",
  FamANDAssem = "FamANDAssem",
  Assembly = "Assembly",
  Family = "Family",
  FamORAssem = "FamORAssem",
  Public = "Public",
  MemberAccessMask = "MemberAccessMask",
  UnmanagedExport = "UnmanagedExport",
  Static = "Static",
  Final = "Final",
  Virtual = "Virtual",
  HideBySig = "HideBySig",
  NewSlot = "NewSlot",
  CheckAccessOnOverride = "CheckAccessOnOverride",
  Abstract = "Abstract",
  SpecialName = "SpecialName",
  RTSpecialName = "RTSpecialName",
  PinvokeImpl = "PinvokeImpl",
  HasSecurity = "HasSecurity",
  RequireSecObject = "RequireSecObject",
  ReservedMask = "ReservedMask",
}

export interface MethodBase {
  memberType?: MemberTypes;
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  attributes?: MethodAttributes;
  methodImplementationFlags?: MethodImplAttributes;
  callingConvention?: CallingConventions;
  isAbstract?: boolean;
  isConstructor?: boolean;
  isFinal?: boolean;
  isHideBySig?: boolean;
  isSpecialName?: boolean;
  isStatic?: boolean;
  isVirtual?: boolean;
  isAssembly?: boolean;
  isFamily?: boolean;
  isFamilyAndAssembly?: boolean;
  isFamilyOrAssembly?: boolean;
  isPrivate?: boolean;
  isPublic?: boolean;
  isConstructedGenericMethod?: boolean;
  isGenericMethod?: boolean;
  isGenericMethodDefinition?: boolean;
  containsGenericParameters?: boolean;
  methodHandle?: RuntimeMethodHandle;
  isSecurityCritical?: boolean;
  isSecuritySafeCritical?: boolean;
  isSecurityTransparent?: boolean;
}

export enum MethodImplAttributes {
  IL = "IL",
  Native = "Native",
  OPTIL = "OPTIL",
  CodeTypeMask = "CodeTypeMask",
  ManagedMask = "ManagedMask",
  NoInlining = "NoInlining",
  ForwardRef = "ForwardRef",
  Synchronized = "Synchronized",
  NoOptimization = "NoOptimization",
  PreserveSig = "PreserveSig",
  AggressiveInlining = "AggressiveInlining",
  AggressiveOptimization = "AggressiveOptimization",
  InternalCall = "InternalCall",
  MaxMethodImplVal = "MaxMethodImplVal",
}

export interface MethodInfo {
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  attributes?: MethodAttributes;
  methodImplementationFlags?: MethodImplAttributes;
  callingConvention?: CallingConventions;
  isAbstract?: boolean;
  isConstructor?: boolean;
  isFinal?: boolean;
  isHideBySig?: boolean;
  isSpecialName?: boolean;
  isStatic?: boolean;
  isVirtual?: boolean;
  isAssembly?: boolean;
  isFamily?: boolean;
  isFamilyAndAssembly?: boolean;
  isFamilyOrAssembly?: boolean;
  isPrivate?: boolean;
  isPublic?: boolean;
  isConstructedGenericMethod?: boolean;
  isGenericMethod?: boolean;
  isGenericMethodDefinition?: boolean;
  containsGenericParameters?: boolean;
  methodHandle?: RuntimeMethodHandle;
  isSecurityCritical?: boolean;
  isSecuritySafeCritical?: boolean;
  isSecurityTransparent?: boolean;
  memberType?: MemberTypes;
  returnParameter?: ParameterInfo;
  returnType?: Type;
  returnTypeCustomAttributes?: ICustomAttributeProvider;
}

export interface MetricDataCollection {
  timeSeries: DateTimeDoubleValueTuple[];
}

export interface Module {
  assembly?: Assembly;
  fullyQualifiedName?: string | null;
  name?: string | null;
  /** @format int32 */
  mdStreamVersion?: number;
  /** @format uuid */
  moduleVersionId?: string;
  scopeName?: string | null;
  moduleHandle?: ModuleHandle;
  customAttributes?: CustomAttributeData[] | null;
  /** @format int32 */
  metadataToken?: number;
}

export interface ModuleHandle {
  /** @format int32 */
  mdStreamVersion?: number;
}

export enum ParameterAttributes {
  None = "None",
  In = "In",
  Out = "Out",
  Lcid = "Lcid",
  Retval = "Retval",
  Optional = "Optional",
  HasDefault = "HasDefault",
  HasFieldMarshal = "HasFieldMarshal",
  Reserved3 = "Reserved3",
  Reserved4 = "Reserved4",
  ReservedMask = "ReservedMask",
}

export interface ParameterInfo {
  attributes?: ParameterAttributes;
  member?: MemberInfo;
  name?: string | null;
  parameterType?: Type;
  /** @format int32 */
  position?: number;
  isIn?: boolean;
  isLcid?: boolean;
  isOptional?: boolean;
  isOut?: boolean;
  isRetval?: boolean;
  defaultValue?: any;
  rawDefaultValue?: any;
  hasDefaultValue?: boolean;
  customAttributes?: CustomAttributeData[] | null;
  /** @format int32 */
  metadataToken?: number;
}

export interface PermissionConfiguration {
  /** @format uuid */
  id?: string;
  permission?: PermissionType;
  userEmail?: string | null;
  environment?: string | null;
  tenant?: string | null;
}

export interface PermissionDetails {
  permission: PermissionType;
  /** @minLength 1 */
  userEmail: string;
  environment?: string | null;
  tenant?: string | null;
}

export enum PermissionType {
  Admin = "Admin",
  Standard = "Standard",
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

export enum PropertyAttributes {
  None = "None",
  SpecialName = "SpecialName",
  RTSpecialName = "RTSpecialName",
  HasDefault = "HasDefault",
  Reserved2 = "Reserved2",
  Reserved3 = "Reserved3",
  Reserved4 = "Reserved4",
  ReservedMask = "ReservedMask",
}

export interface PropertyInfo {
  name?: string | null;
  declaringType?: Type;
  reflectedType?: Type;
  module?: Module;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  memberType?: MemberTypes;
  propertyType?: Type;
  attributes?: PropertyAttributes;
  isSpecialName?: boolean;
  canRead?: boolean;
  canWrite?: boolean;
  getMethod?: MethodInfo;
  setMethod?: MethodInfo;
}

export interface ResourceNotFoundException {
  targetSite?: MethodBase;
  message?: string | null;
  data?: Record<string, any>;
  innerException?: Exception;
  helpLink?: string | null;
  source?: string | null;
  /** @format int32 */
  hResult?: number;
  stackTrace?: string | null;
  status?: HttpStatusCode;
  typeName?: string | null;
  resourceId?: any;
  problemType?: string | null;
}

export interface RuntimeFieldHandle {
  value?: IntPtr;
}

export interface RuntimeMethodHandle {
  value?: IntPtr;
}

export interface RuntimeTypeHandle {
  value?: IntPtr;
}

export enum SecurityRuleSet {
  None = "None",
  Level1 = "Level1",
  Level2 = "Level2",
}

export interface ServiceAlert {
  /** @minLength 1 */
  name: string;
  threshold: HealthStatus;
  /** @minLength 1 */
  receiverName: string;
  receiverType: AlertReceiverType;
  /** @format date-time */
  since?: string | null;
  isFiring: boolean;
  isSilenced: boolean;
  silenceDetails?: AlertSilenceView;
}

export interface ServiceConfiguration {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  /** @minLength 1 */
  displayName: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  healthChecks?: HealthCheckModel[] | null;
  versionChecks?: VersionCheckModel[] | null;
  children?: string[] | null;
  tags?: Record<string, string>;
  alertingRules?: AlertingRuleConfiguration[] | null;
}

export interface ServiceHealth {
  /** @format date-time */
  timestamp: string;
  aggregateStatus: HealthStatus;
  healthChecks: Record<string, HealthStatus>;
}

export interface ServiceHealthData {
  healthCheckSamples: Record<string, DateTimeDoubleValueTuple[]>;
  /** @format int32 */
  totalHealthChecks?: number;
  /** @format int32 */
  totalSamples?: number;
}

export interface ServiceHierarchyConfiguration {
  services: ServiceConfiguration[];
  rootServices: string[];
  tags?: Record<string, string>;
  alerting?: AlertingConfiguration;
}

export interface ServiceHierarchyHealth {
  /** @minLength 1 */
  name: string;
  /** @minLength 1 */
  displayName: string;
  /** @minLength 1 */
  dashboardLink: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
  healthChecks?: Record<string, DateTimeHealthStatusValueTuple>;
  children?: ServiceHierarchyHealth[] | null;
  tags?: Record<string, string>;
  isInMaintenance?: boolean;
  inMaintenanceTypes?: string | null;
}

export interface ServiceHierarchyHealthHistory {
  /** @minLength 1 */
  name: string;
  /** @minLength 1 */
  displayName: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  aggregateStatus?: DateTimeHealthStatusValueTuple[] | null;
  children?: ServiceHierarchyHealthHistory[] | null;
}

export interface ServiceHierarchyInfo {
  /** @minLength 1 */
  name: string;
  /** @minLength 1 */
  displayName: string;
  /** @minLength 1 */
  dashboardLink: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
  versions?: Record<string, string>;
  healthChecks?: Record<string, DateTimeHealthStatusValueTuple>;
  children?: ServiceHierarchyInfo[] | null;
  tags?: Record<string, string>;
  isInMaintenance?: boolean;
  inMaintenanceTypes?: string | null;
}

export interface ServiceVersion {
  /** @format date-time */
  timestamp: string;
  versionChecks: Record<string, string>;
}

export interface ServiceVersionDetails {
  versionType: VersionCheckType;
  /** @minLength 1 */
  version: string;
  /** @format date-time */
  timestamp: string;
}

export interface ServiceVersionHistory {
  /** @minLength 1 */
  name: string;
  /** @minLength 1 */
  displayName: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  versionHistory?: DateTimeServiceVersionTypeInfoIImmutableListValueTuple[] | null;
}

export interface ServiceVersionTypeInfo {
  versionType: VersionCheckType;
  /** @minLength 1 */
  version: string;
}

export interface StructLayoutAttribute {
  typeId?: any;
  value?: LayoutKind;
}

export interface TenantInfo {
  /** @minLength 1 */
  environmentName: string;
  /** @minLength 1 */
  tenantName: string;
  isNonProd: boolean;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
  rootServices?: ServiceHierarchyInfo[] | null;
  isInMaintenance?: boolean;
  inMaintenanceTypes?: string | null;
}

export interface TimeSpan {
  /** @format int64 */
  ticks?: number;
  /** @format int32 */
  days?: number;
  /** @format int32 */
  hours?: number;
  /** @format int32 */
  milliseconds?: number;
  /** @format int32 */
  microseconds?: number;
  /** @format int32 */
  nanoseconds?: number;
  /** @format int32 */
  minutes?: number;
  /** @format int32 */
  seconds?: number;
  /** @format double */
  totalDays?: number;
  /** @format double */
  totalHours?: number;
  /** @format double */
  totalMilliseconds?: number;
  /** @format double */
  totalMicroseconds?: number;
  /** @format double */
  totalNanoseconds?: number;
  /** @format double */
  totalMinutes?: number;
  /** @format double */
  totalSeconds?: number;
}

export interface Type {
  name?: string | null;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  isInterface?: boolean;
  memberType?: MemberTypes;
  namespace?: string | null;
  assemblyQualifiedName?: string | null;
  fullName?: string | null;
  assembly?: Assembly;
  module?: Module;
  isNested?: boolean;
  declaringType?: Type;
  declaringMethod?: MethodBase;
  reflectedType?: Type;
  underlyingSystemType?: Type;
  isTypeDefinition?: boolean;
  isArray?: boolean;
  isByRef?: boolean;
  isPointer?: boolean;
  isConstructedGenericType?: boolean;
  isGenericParameter?: boolean;
  isGenericTypeParameter?: boolean;
  isGenericMethodParameter?: boolean;
  isGenericType?: boolean;
  isGenericTypeDefinition?: boolean;
  isSZArray?: boolean;
  isVariableBoundArray?: boolean;
  isByRefLike?: boolean;
  isFunctionPointer?: boolean;
  isUnmanagedFunctionPointer?: boolean;
  hasElementType?: boolean;
  genericTypeArguments?: Type[] | null;
  /** @format int32 */
  genericParameterPosition?: number;
  genericParameterAttributes?: GenericParameterAttributes;
  attributes?: TypeAttributes;
  isAbstract?: boolean;
  isImport?: boolean;
  isSealed?: boolean;
  isSpecialName?: boolean;
  isClass?: boolean;
  isNestedAssembly?: boolean;
  isNestedFamANDAssem?: boolean;
  isNestedFamily?: boolean;
  isNestedFamORAssem?: boolean;
  isNestedPrivate?: boolean;
  isNestedPublic?: boolean;
  isNotPublic?: boolean;
  isPublic?: boolean;
  isAutoLayout?: boolean;
  isExplicitLayout?: boolean;
  isLayoutSequential?: boolean;
  isAnsiClass?: boolean;
  isAutoClass?: boolean;
  isUnicodeClass?: boolean;
  isCOMObject?: boolean;
  isContextful?: boolean;
  isEnum?: boolean;
  isMarshalByRef?: boolean;
  isPrimitive?: boolean;
  isValueType?: boolean;
  isSignatureType?: boolean;
  isSecurityCritical?: boolean;
  isSecuritySafeCritical?: boolean;
  isSecurityTransparent?: boolean;
  structLayoutAttribute?: StructLayoutAttribute;
  typeInitializer?: ConstructorInfo;
  typeHandle?: RuntimeTypeHandle;
  /** @format uuid */
  guid?: string;
  baseType?: Type;
  /** @deprecated */
  isSerializable?: boolean;
  containsGenericParameters?: boolean;
  isVisible?: boolean;
}

export enum TypeAttributes {
  NotPublic = "NotPublic",
  Public = "Public",
  NestedPublic = "NestedPublic",
  NestedPrivate = "NestedPrivate",
  NestedFamily = "NestedFamily",
  NestedAssembly = "NestedAssembly",
  NestedFamANDAssem = "NestedFamANDAssem",
  VisibilityMask = "VisibilityMask",
  SequentialLayout = "SequentialLayout",
  ExplicitLayout = "ExplicitLayout",
  LayoutMask = "LayoutMask",
  Interface = "Interface",
  Abstract = "Abstract",
  Sealed = "Sealed",
  SpecialName = "SpecialName",
  RTSpecialName = "RTSpecialName",
  Import = "Import",
  Serializable = "Serializable",
  WindowsRuntime = "WindowsRuntime",
  UnicodeClass = "UnicodeClass",
  AutoClass = "AutoClass",
  StringFormatMask = "StringFormatMask",
  HasSecurity = "HasSecurity",
  ReservedMask = "ReservedMask",
  BeforeFieldInit = "BeforeFieldInit",
  CustomFormatMask = "CustomFormatMask",
}

export interface TypeInfo {
  name?: string | null;
  customAttributes?: CustomAttributeData[] | null;
  isCollectible?: boolean;
  /** @format int32 */
  metadataToken?: number;
  isInterface?: boolean;
  memberType?: MemberTypes;
  namespace?: string | null;
  assemblyQualifiedName?: string | null;
  fullName?: string | null;
  assembly?: Assembly;
  module?: Module;
  isNested?: boolean;
  declaringType?: Type;
  declaringMethod?: MethodBase;
  reflectedType?: Type;
  underlyingSystemType?: Type;
  isTypeDefinition?: boolean;
  isArray?: boolean;
  isByRef?: boolean;
  isPointer?: boolean;
  isConstructedGenericType?: boolean;
  isGenericParameter?: boolean;
  isGenericTypeParameter?: boolean;
  isGenericMethodParameter?: boolean;
  isGenericType?: boolean;
  isGenericTypeDefinition?: boolean;
  isSZArray?: boolean;
  isVariableBoundArray?: boolean;
  isByRefLike?: boolean;
  isFunctionPointer?: boolean;
  isUnmanagedFunctionPointer?: boolean;
  hasElementType?: boolean;
  genericTypeArguments?: Type[] | null;
  /** @format int32 */
  genericParameterPosition?: number;
  genericParameterAttributes?: GenericParameterAttributes;
  attributes?: TypeAttributes;
  isAbstract?: boolean;
  isImport?: boolean;
  isSealed?: boolean;
  isSpecialName?: boolean;
  isClass?: boolean;
  isNestedAssembly?: boolean;
  isNestedFamANDAssem?: boolean;
  isNestedFamily?: boolean;
  isNestedFamORAssem?: boolean;
  isNestedPrivate?: boolean;
  isNestedPublic?: boolean;
  isNotPublic?: boolean;
  isPublic?: boolean;
  isAutoLayout?: boolean;
  isExplicitLayout?: boolean;
  isLayoutSequential?: boolean;
  isAnsiClass?: boolean;
  isAutoClass?: boolean;
  isUnicodeClass?: boolean;
  isCOMObject?: boolean;
  isContextful?: boolean;
  isEnum?: boolean;
  isMarshalByRef?: boolean;
  isPrimitive?: boolean;
  isValueType?: boolean;
  isSignatureType?: boolean;
  isSecurityCritical?: boolean;
  isSecuritySafeCritical?: boolean;
  isSecurityTransparent?: boolean;
  structLayoutAttribute?: StructLayoutAttribute;
  typeInitializer?: ConstructorInfo;
  typeHandle?: RuntimeTypeHandle;
  /** @format uuid */
  guid?: string;
  baseType?: Type;
  /** @deprecated */
  isSerializable?: boolean;
  containsGenericParameters?: boolean;
  isVisible?: boolean;
  genericTypeParameters?: Type[] | null;
  declaredConstructors?: ConstructorInfo[] | null;
  declaredEvents?: EventInfo[] | null;
  declaredFields?: FieldInfo[] | null;
  declaredMembers?: MemberInfo[] | null;
  declaredMethods?: MethodInfo[] | null;
  declaredNestedTypes?: TypeInfo[] | null;
  declaredProperties?: PropertyInfo[] | null;
  implementedInterfaces?: Type[] | null;
}

export interface UptimeModel {
  /** @minLength 1 */
  name: string;
  /** @format double */
  percentUptime: number;
  totalUptime: TimeSpan;
  currentUptime: TimeSpan;
  unknownDuration: TimeSpan;
  children: UptimeModel[];
}

export interface UserPermissionsView {
  permissionTree?: Record<string, string[]>;
}

export type VersionCheckDefinition = object;

export interface VersionCheckModel {
  versionCheckType: VersionCheckType;
  definition: VersionCheckDefinition;
}

export enum VersionCheckType {
  FluxKustomization = "FluxKustomization",
  HttpResponseBody = "HttpResponseBody",
  KubernetesImage = "KubernetesImage",
  FluxHelmRelease = "FluxHelmRelease",
}
