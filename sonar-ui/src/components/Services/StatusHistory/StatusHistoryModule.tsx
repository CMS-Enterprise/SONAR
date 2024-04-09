import { DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import { ArrowIcon, Spinner } from '@cmsgov/design-system';
import React, { useCallback, useContext, useEffect, useMemo, useState } from 'react';
import {
  DateTimeHealthStatusValueTuple,
  ServiceHierarchyHealth
} from 'api/data-contracts';
import ThemedDropdown from 'components/Common/ThemedDropdown';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import {
  StatusStartTimes,
  statusTimeRangeOptions
} from 'helpers/DropdownOptions';
import {
  calculateQueryStep,
  getStartDate,
  getNewRangeBasedDate
} from 'helpers/StatusHistoryHelper';
import { ServiceOverviewHeaderStyle } from '../ServiceOverview.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { useGetServiceHealthHistory } from '../Services.Hooks';
import StatusHistoryDatePicker from './StatusHistoryDatePicker';
import StatusHistoryQuickRange from './StatusHistoryQuickRange';
import StatusHistoryRangeInfo from './StatusHistoryRangeInfo';
import StatusHistoryTile from './StatusHistoryTile';
import {
  StatusHistoryButtonStyle,
  StatusHistoryTileContainerStyle,
  StatusHistoryTimeRangeContainerStyle,
  StatusHistoryTimeRangeOptionStyle,
} from './StatusHistory.Style';

const StatusHistoryModule: React.FC<{
  addTimestamp: (
    tupleData: DateTimeHealthStatusValueTuple,
    tileId: string,
    serviceData: ServiceHierarchyHealth
  ) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  servicePath: string,
  serviceHealth: ServiceHierarchyHealth,
  setRangeInSeconds: React.Dispatch<React.SetStateAction<number>>
}> =
  ({
    addTimestamp,
    closeDrawer,
    selectedTileId,
    servicePath,
    serviceHealth,
    setRangeInSeconds
  }) => {
    const context = useContext(ServiceOverviewContext)!;
    const currentDateTime = useMemo(() => {
      return new Date();
    }, []);
    const millisecondsPerDay = 86400000; // 24h * 60min/h * 60sec/min * 1000ms/sec
    const earliestStartDateTime = useMemo(() => {
      return new Date(currentDateTime.getTime() - 15 * millisecondsPerDay);
    }, [currentDateTime]);
    const initialStartDateTime = getStartDate(StatusStartTimes.Last6Hours, currentDateTime);

    // If current range includes more than 1 date, display date on status history tile tooltip
    const [diffDates, setDiffDates] = useState(false);

    // Quick range (i.e. ranging from 6 hours to 1 week) or custom range for status history
    const [selectedTimeRangeOption, setTimeRangeOption] = useState<DropdownValue>(StatusStartTimes.Last6Hours);
    const [isCustomRange, setIsCustomRange] = useState<boolean>(false);

    // The start and end date selected for a service's status history
    const [statusStartDate, setStatusStartDate] = useState<Date>(initialStartDateTime);
    const [statusEndDate, setStatusEndDate] = useState<Date>(currentDateTime);

    // The range between the current start and end dates
    const [rangeInMsec, setRangeInMsec] = useState<number>(statusEndDate.getTime() - statusStartDate.getTime());

    // The range in time associated with each displayed status history tile
    // in the current view
    const [queryStepInSeconds, setQueryStep] = useState<number>(calculateQueryStep(rangeInMsec));

    // Disable/enable the button(s) for going back and/or forth in time
    const [disablePrevious, setDisablePrevious] = useState<boolean>(false);
    const [disableNext, setDisableNext] = useState<boolean>(true);

    // Display status history tile(s) if both start and end dates are selected
    const [displayTiles, setDisplayTiles] = useState<boolean>(true);

    // Refetch query only if true
    const [refreshData, setRefreshData] = useState<boolean>(false);

    const { isFetching, isError, refetch, data } =
      useGetServiceHealthHistory(
        context.environmentName,
        context.tenantName,
        servicePath,
        statusStartDate,
        statusEndDate,
        queryStepInSeconds
      );

    // This handler is triggered when a user clicks on the button for going
    // backwards or forwards in time (given the current date/time range and
    // step) and updates the start and end date/time appropriately and
    // triggers a data refetch.
    const handleRangeUpdate = (goToPrevious: boolean) => {
      if (goToPrevious) {
        const prevStartDate = statusStartDate;
        const prevEndDate = statusEndDate;

        setStatusStartDate(getNewRangeBasedDate(prevStartDate, -rangeInMsec));
        setStatusEndDate(getNewRangeBasedDate(prevEndDate, -rangeInMsec));
      } else {
        setStatusStartDate(getNewRangeBasedDate(statusStartDate, rangeInMsec));
        setStatusEndDate(getNewRangeBasedDate(statusEndDate, rangeInMsec));
      }
      setRefreshData(true);
    }

    // This hook is triggered after a query refetch. Given the current status
    // history date/time range, if going backwards in time involves
    // dates prior to the earliest choosable start date, the previous button
    // is disabled. If going forwards in time involves dates beyond the current
    // date and time, the next button is disabled.
    const updatePrevNextButtons = useCallback(() => {
      setRangeInSeconds(rangeInMsec / 1000);

      const potentialStartDate = getNewRangeBasedDate(statusStartDate, -rangeInMsec);
      if (potentialStartDate.getTime() < earliestStartDateTime.getTime()) {
        setDisablePrevious(true);
      } else {
        setDisablePrevious(false);
      }

      const potentialNextEndDate = getNewRangeBasedDate(statusEndDate, rangeInMsec);
      const currDateTime = new Date();
      if (potentialNextEndDate.getTime() > currDateTime.getTime()) {
        setDisableNext(true);
      } else {
        setDisableNext(false);
      }
    }, [earliestStartDateTime, statusStartDate, statusEndDate, rangeInMsec, setRangeInSeconds]);

    // This hook is triggered whenever the data returned from the query changes.
    // If the query's range includes multiple dates, the current date string
    // associated with an aggregate status will include the date; if not, the
    // current date string associated will only include the time.
    useEffect(() => {
      let currDate = '';
      if (data?.aggregateStatus) {
        for (let i = 0; i < data?.aggregateStatus?.length; i++) {
          const currItem = data?.aggregateStatus[i];
          const localDateString = new Date(currItem[0]).toDateString();
          if (currDate !== '') {
            if (currDate !== localDateString) {
              setDiffDates(true);
              break;
            }
          }
          currDate = localDateString;
        }
      }
    }, [data])

    // This handler is triggered when a user selects an option from the time range
    // dropdown. If the option chosen is not the custom range option, a data
    // refetch occurs; otherwise, date/time pickers for a custom start and end date
    // range is displayed.
    const handleSelectTimeRangeOption = (selectedTimeRangeOption: DropdownValue) => {
      setTimeRangeOption(selectedTimeRangeOption);
      if (+selectedTimeRangeOption !== StatusStartTimes.CustomTimeRange) {
        setIsCustomRange(false);

        const currDate = new Date();
        const start = getStartDate(+selectedTimeRangeOption, currDate);
        setStatusStartDate(getStartDate(+selectedTimeRangeOption, currDate));
        setStatusEndDate(currDate);

        const range = currDate.getTime() - start.getTime();
        const step = calculateQueryStep(range);
        setRangeInMsec(range);
        setQueryStep(step);
        setRefreshData(true);
      } else {
        setIsCustomRange(true);

        setRefreshData(false);
        setDisablePrevious(true);
        setDisableNext(true);
        setDisplayTiles(false);
      }
    }

    // A data refetch occurs when the user does any of the following:
    // A. selects a quick range option from the time range dropdown
    // B. clicks the Query button after selecting a custom start and end date
    // C. clicks on the button for going backwards or forwards in time (given
    //    the current date/time range)
    useEffect(() => {
      if (refreshData) {
        refetch();
      }
    }, [refreshData, refetch]);

    // This hook is triggered after a data refetch occurs and subsequently
    // disables or enables the buttons for going backwards or forwards in
    // time and displays the status history tiles.
    useEffect(() => {
      if (!isFetching) {
        updatePrevNextButtons();
        setDisplayTiles(true);
        setRefreshData(false);
      }
    }, [isFetching, updatePrevNextButtons]);

    return (
      <>
        <div css={ServiceOverviewHeaderStyle}>
          Status History
        </div>

        <div className="ds-l-row">
          <ThemedDropdown
            label=""
            ariaLabel="Status History Time Range Options"
            name="status_time_range"
            onChange={(event) => handleSelectTimeRangeOption(event.target.value)}
            value={selectedTimeRangeOption}
            options={statusTimeRangeOptions}
            css={StatusHistoryTimeRangeOptionStyle}
          />
        </div>

        <div
          className="ds-l-row"
          css={StatusHistoryTimeRangeContainerStyle}
        >
          <PrimaryActionButton
            size={"small"}
            disabled={disablePrevious}
            onClick={() => handleRangeUpdate(true)}
            css={StatusHistoryButtonStyle}
          >
            <ArrowIcon direction='left' />
          </PrimaryActionButton>
          <PrimaryActionButton
            size={"small"}
            disabled={disableNext}
            onClick={() => handleRangeUpdate(false)}
            css={StatusHistoryButtonStyle}
          >
            <ArrowIcon direction='right' />
          </PrimaryActionButton>
          {isCustomRange ?
            <StatusHistoryDatePicker
              setStatusStartDate={setStatusStartDate}
              setStatusEndDate={setStatusEndDate}
              setRangeInMsec={setRangeInMsec}
              setQueryStep={setQueryStep}
              setRefreshData={setRefreshData}
              earliestChoosableDate={earliestStartDateTime}
              currentStartDate={statusStartDate}
              currentEndDate={statusEndDate}
            /> :
            <StatusHistoryQuickRange
              statusStartDate={statusStartDate}
              statusEndDate={statusEndDate}
            />
          }
        </div>

        <div
          className="ds-l-row"
          css={StatusHistoryTimeRangeContainerStyle}
        >
          {isCustomRange ?
            (displayTiles ?
              <StatusHistoryRangeInfo
                isCustomRange={true}
                rangeInMsec={rangeInMsec}
                queryStepInSec={queryStepInSeconds}
              />
              : null) :
            <StatusHistoryRangeInfo
              isCustomRange={false}
              rangeInMsec={rangeInMsec}
              queryStepInSec={queryStepInSeconds}
            />
          }
        </div>

        {!isError ?
          (isFetching ? (<Spinner />) : (
            <div css={StatusHistoryTileContainerStyle}>
              {displayTiles ? (
                <div>
                  {data?.aggregateStatus?.map((item, index) => (
                    <StatusHistoryTile
                      key={`${serviceHealth.name}-${index}`}
                      id={`${serviceHealth.name}-${index}`}
                      statusTimestampTuple={item}
                      addTimestamp={addTimestamp}
                      closeDrawer={closeDrawer}
                      selectedTileId={selectedTileId}
                      serviceHealth={serviceHealth}
                      showDate={diffDates}
                      envName={context.environmentName}
                      tenantName={context.tenantName}
                      servicePath={servicePath}
                      rangeInSeconds={rangeInMsec / 1000}
                    />
                  ))}
                </div>
              ) : null}
            </div>
          )) : (
          <div css={StatusHistoryTileContainerStyle}>
            Unable to fetch data.
          </div>
        )}
      </>
    );
  };

export default StatusHistoryModule;
