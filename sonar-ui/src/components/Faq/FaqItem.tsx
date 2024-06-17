import { Accordion, AccordionItem, ArrowIcon } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { FaqContent } from '../../types';
import { getFaqItemStyle } from './FaqItem.Style';

const FaqItem: React.FC<{
  faqContent: FaqContent
}> = ({faqContent}) => {
  const theme = useTheme();
  return (
    <Accordion bordered css={getFaqItemStyle(theme)}>
      <AccordionItem
          heading={faqContent.header}
          closeIcon={<ArrowIcon direction={"up"} />}
          openIcon={<ArrowIcon direction={"down"} />}
      >
        {faqContent.body}
      </AccordionItem>
    </Accordion>
  )
}

export default FaqItem;
