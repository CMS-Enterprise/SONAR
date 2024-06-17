import React from 'react';
import { pageTitleStyle } from '../App.Style';
import { EnvironmentItemContainerStyle } from '../components/Environments/EnvironmentItem.Style';
import FaqItem from '../components/Faq/FaqItem';
import { FaqContent } from '../types';
import { ToolTipText } from '../utils/constants';

const Faqs: FaqContent[] = [
  {
    header: "What does SONAR do?",
    body: "SONAR monitors services across multiple environments and applications and centralizes information about their health in a single data store. This enables end users and integrators to easily determine the health of these applications from a single dashboard or API integration."
  },
  {
    header: "What is an Environment?",
    body: ToolTipText.environmentTip
  },
  {
    header: "What is a Tenant?",
    body: "A Tenant is a grouping of services, generally meant to imply it is owned by a specific team. Services within a tenant may be interdependent, but may not be dependent on services with another tenant."
  },
  {
    header: "What is a Service?",
    body: "A Service can be a conceptual grouping of applications for a particular business function or area, or a specific application component. A Service may have a user facing URL or it may be completely internal. Services may be interdependent, but there should never be a cyclic dependency. A Service’s health may be determined based on some Health Checks, the health of its dependencies, or both. Each tenant should have on or more “Root Services” which would represent the top of the conceptual hierarchy of application components."
  },
  {
    header: "How does SONAR determine a service's aggregate status?",
    body: "The Health Status of a Service is determined by aggregating the Health Status of its child Services, if there are any, along with the Status of its Health Checks, if there are any. When Health Status is aggregated, The result is the most severe status (where “Unknown” is the most severe and “Online” is the least severe). So if any one of a Service’s Health Checks has the status of “Offline” then that service will be considered “Offline.” Likewise for child Services."
  }
]

const Faq = () => {
  return (
    <div
      className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto"
      css={EnvironmentItemContainerStyle}
      data-test="env-view-accordion">
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
        <div
          className="ds-l-col--4 ds-u-margin-right--auto ds-u-margin-left--auto ds-u-margin-y--3"
          css={pageTitleStyle}
        >
          Frequently Asked Questions
        </div>
      </div>
      {Faqs.map(f => (
        <div css={{margin: 20}} key={f.header}>
          <FaqItem faqContent={f} />
        </div>
      ))}
    </div>
  )
}

export default Faq;
