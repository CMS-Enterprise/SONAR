import { BaseOptions } from 'flatpickr/dist/types/options';
import React, { useEffect, useState } from 'react';
import Flatpickr from 'react-flatpickr';
import "flatpickr/dist/themes/light.css";
import PrimaryActionButton from '../../Common/PrimaryActionButton';
import { StatusHistoryDatePickerContainerStyle } from './StatusHistory.Style';
import { calculateQueryStep } from 'helpers/StatusHistoryHelper';

const StatusHistoryDatePicker: React.FC<{
  setStatusStartDate: React.Dispatch<React.SetStateAction<Date>>,
  setStatusEndDate: React.Dispatch<React.SetStateAction<Date>>,
  setRangeInMsec: React.Dispatch<React.SetStateAction<number>>,
  setQueryStep: React.Dispatch<React.SetStateAction<number>>,
  setRefreshData: React.Dispatch<React.SetStateAction<boolean>>,
  earliestChoosableDate: Date,
  currentStartDate: Date,
  currentEndDate: Date
}> = ({
  setStatusStartDate,
  setStatusEndDate,
  setRangeInMsec,
  setQueryStep,
  setRefreshData,
  earliestChoosableDate,
  currentStartDate,
  currentEndDate
}) => {
  const [chosenStartDate, setChosenStartDate] = useState<Date>();
  const [chosenEndDate, setChosenEndDate] = useState<Date>();
  const [submitDisabled, setSubmitDisabled] = useState(true);
  const [userHasSelectedStart, setUserHasSelectedStart] = useState<boolean>(false);
  const [userHasSelectedEnd, setUserHasSelectedEnd] = useState<boolean>(false);
  const getPickerOptions = (isStartDatePicker: boolean): Partial<BaseOptions> => {
    const currentDate = new Date();
    let maxDate = new Date(currentDate.getTime());
    let minDate = new Date(earliestChoosableDate.getTime());
    const millisecondsPerMin = 60 * 1000; // 60sec/min * 1000 ms/sec
    const millisecondsPerWeek = 7 * 24 * 60 * millisecondsPerMin; // 7days * 24h/day * 60min/h * 60sec/min * 1000 ms/sec

    if (isStartDatePicker) {
      maxDate.setHours(maxDate.getHours() - 1);
    } else {
      if (chosenStartDate) {
        // Adjust earliest and latest choosable date/time based on the selected start date/time
        minDate = new Date(chosenStartDate.getTime() + millisecondsPerMin);

        // End - Start cannot be greater than 7 days to be consistent with Metric history restriction.
        const potentialMaxDate = new Date(chosenStartDate.getTime() + millisecondsPerWeek);
        if (potentialMaxDate <= currentDate) {
          maxDate = new Date(potentialMaxDate);
        }
        maxDate = (potentialMaxDate <= currentDate) ?
          maxDate = new Date(potentialMaxDate) :
          maxDate = new Date(currentDate);
      }
    }

    return {
      dateFormat: "n/j/Y h:i:S K",
      defaultHour: currentDate.getHours(),
      defaultMinute: currentDate.getMinutes(),
      minDate: minDate,
      maxDate: maxDate
    }
  }

  useEffect(() => {
    setChosenStartDate(currentStartDate);
    setChosenEndDate(currentEndDate);
  }, [currentStartDate, currentEndDate]);

  // Display or update displayed status history tiles only when Query button is clicked
  const handleQuery = () => {
    setStatusStartDate(chosenStartDate!);
    setStatusEndDate(chosenEndDate!);

    const range = chosenEndDate!.getTime() - chosenStartDate!.getTime();
    const step = calculateQueryStep(range);
    setRangeInMsec(range);
    setQueryStep(step);
    setRefreshData(true);
  }

  // Hook to update disabled state of query submit button
  useEffect(() => {
    if ((chosenStartDate == null) ||
      (chosenEndDate == null) ||
      !userHasSelectedStart ||
      !userHasSelectedEnd) {
      setSubmitDisabled(true);
    } else {
      setSubmitDisabled(false);
    }
  }, [chosenStartDate, chosenEndDate, userHasSelectedStart, userHasSelectedEnd]);

  return (
    <div css={StatusHistoryDatePickerContainerStyle}>
      <Flatpickr
        placeholder={"Start Date & Time"}
        data-enable-time
        value={!userHasSelectedStart ? undefined : chosenStartDate}
        onChange={([selectedDate]) => {
          setChosenStartDate(selectedDate);
          setUserHasSelectedStart(true);
        }}
        options={getPickerOptions(true)}
      />
      <span>to</span>
      <Flatpickr
        placeholder={"End Date & Time"}
        data-enable-time
        disabled={userHasSelectedStart ? false : true}
        value={!userHasSelectedEnd ? undefined : chosenEndDate}
        onChange={([selectedDate]) => {
          setChosenEndDate(selectedDate);
          setUserHasSelectedEnd(true);
        }}
        options={getPickerOptions(false)}
      />
      <PrimaryActionButton
        size={"small"}
        disabled={submitDisabled}
        onClick={handleQuery}
      >Query
      </PrimaryActionButton>
    </div>
  )
}

export default StatusHistoryDatePicker;
