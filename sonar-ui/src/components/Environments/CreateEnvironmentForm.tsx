import { useOktaAuth } from '@okta/okta-react';
import React, { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from 'react-query';
import { EnvironmentModel } from '../../api/data-contracts';
import { HttpResponse } from '../../api/http-client';
import { useSonarApi } from 'components/SonarApi/Provider';
import AlertBanner from '../App/AlertBanner';
import PrimaryActionButton from '../Common/PrimaryActionButton';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import ThemedTextField from '../Common/ThemedTextField';

function isValidInput(str: string) {
  return /^[a-zA-Z0-9_-]*$/.test(str);
}

const CreateEnvironmentForm: React.FC<{
  handleModalToggle: () => void
}> = ({
  handleModalToggle
}) => {
  const sonarClient = useSonarApi();
  const queryClient = useQueryClient();
  const { oktaAuth } = useOktaAuth();
  const [environmentName, setEnvironmentName] = useState("")
  const [alertHeading, setAlertHeading] = useState("Environment Name is required");
  const [alertText, setAlertText] = useState("Environment Name may only contain letters, numbers, underscores, and hyphens");
  const [submitDisabled, setSubmitDisabled] = useState(true);
  const mutation = useMutation({
    mutationFn: (newEnv: EnvironmentModel) => sonarClient.createEnvironment(newEnv, {
      headers: {
        'Authorization': `Bearer ${oktaAuth.getIdToken()}`
      }
    }),
    onSuccess: res => {
      queryClient.invalidateQueries({queryKey: ['environments']});
      handleModalToggle();
    },
    onError: (error: HttpResponse<Error>) => {
      // set error state
      if (error.status === 409) {
        setAlertHeading("Environment already exists");
        setAlertText("The specified Environment already exists. Please try again with a unique Environment name.")
      } else {
        setAlertHeading("Error creating Environment");
        setAlertText("An error occurred while processing your request. Please try again.")
      }
    }
  })

  const handleSubmit = () => {
    const newEnv: EnvironmentModel = {
      name: environmentName
    }
    mutation.mutate(newEnv);
  }

  useEffect(() => {
    setSubmitDisabled(
      !isValidInput(environmentName) ||
      environmentName === "");
  }, [environmentName]);

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
        >
          <ThemedTextField
            name={"environment-name-field"}
            label={"Environment Name:"}
            value={environmentName}
            onChange={(e) => setEnvironmentName(e.target.value)}
          />
        </div>
      </div>

      {
        environmentName === "" ||
        !isValidInput(environmentName) ||
        mutation.isError ? (
          <div className="ds-l-row">
            <div
              className="ds-l-col--12"
            >
              <AlertBanner
                alertHeading={alertHeading}
                alertText={alertText}
                variation={mutation.isError ? "error" : "warn"}
              />
            </div>
          </div>
        ) : null}
      <div className="ds-l-row ds-u-justify-content--end ds-u-margin-top--2">
        <div
          className="ds-l-col--3 ds-u-margin-right--1"
        >
          <SecondaryActionButton
            onClick={handleModalToggle}
          >
            Cancel
          </SecondaryActionButton>
        </div>
        <div
          className="ds-l-col--3"
        >
          <PrimaryActionButton
            onClick={handleSubmit}
            disabled={submitDisabled}
          >
            Add
          </PrimaryActionButton>
        </div>
      </div>

    </section>
  )
}

export default CreateEnvironmentForm;
