import React from 'react';

const VersionInfo: React.FC<{
  versions: Record<string, string> | undefined
}> =
  ({ versions}) => {

    let version = 'undefined';

    if((versions != null) && (Object.keys(versions).length > 0)) {
      const sorted = [];
      for (const key in versions) {
        sorted[sorted.length] = versions[key];
      }
      sorted.sort();
      version = sorted[0];
    }

    return (
      <span>
        ({version})
      </span>
    );
  };

export default VersionInfo;