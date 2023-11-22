import { TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import React from 'react';
import ThemedTable from '../../Common/ThemedTable';
import { ServiceTagContainerStyle } from './ServiceTags.Style';

const ServiceTagsTable: React.FC<{
  tags: Record<string, string>
}> = ({ tags }) => {
  return (
    <>
      <div css={ServiceTagContainerStyle}>
        Tags
      </div>
      <div className='ds-l-row ds-u-align-items--start'>
        <div className='ds-l-col--6'>
          <ThemedTable>
            <TableHead>
              <TableCell>
                Tag Name
              </TableCell>
              <TableCell>
                Tag Value
              </TableCell>
            </TableHead>
            <TableBody>
            {Object.entries(tags).map(tag => (
              <TableRow key={tag[0]}>
                <TableCell>
                  {tag[0]}
                </TableCell>
                <TableCell>
                  {tag[1]}
                </TableCell>
              </TableRow>
            ))}
            </TableBody>
          </ThemedTable>
        </div>
      </div>
    </>
  );
};

export default ServiceTagsTable;
