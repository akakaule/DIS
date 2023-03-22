import { AddIcon, EditIcon } from "@chakra-ui/icons";
import { Box, Divider, GridItem, Grid, SimpleGrid, Table, TableContainer, Tbody, Td, Text, Th, Thead, Tr } from "@chakra-ui/react";
import * as api from "api-client";
import React, { Dispatch, SetStateAction, useState } from "react";
import MetadataButton from "./information-modal";
import "./metadata.css";

interface IMetadataolumnProps {
  metadata: api.Metadata | undefined;
}

const MetadataColumn = (props: IMetadataolumnProps) => {
  const [endpointMetadata, setendpointMetadata] = React.useState<api.Metadata | undefined>(props.metadata);

  const [hasMetaData, sethasMetaData] = React.useState<boolean>(false);
  const [hasTechnicalContact, sethasTechnicalContact] = React.useState<boolean>(false);
  const [feedback, setFeedback] = useState("");
  const timeoutIdRef = React.useRef<NodeJS.Timeout>();
  const [feedbackColour, setFBcolour] = useState("Green");

  React.useEffect(() => {
    if (endpointMetadata) {
      sethasMetaData(true);
      if (endpointMetadata.technicalContacts && endpointMetadata.technicalContacts.length !== 0) {
        sethasTechnicalContact(true);
      }
    } else {
      sethasMetaData(false);
    }
  }, [endpointMetadata]);

  const clearAlertsFeedback = () => {
    setFeedback("");
  };

  const HasMetaData = () => (
    <Box padding={3}>
      <Grid templateColumns="repeat(3, 1fr)">
        <GridItem colSpan={3} textAlign="center" border={"0px"} textColor="black" className="Header">
          Owner Details
        </GridItem>

        <Text className="Header">Owner team</Text>
        <GridItem colSpan={2} className="Cell" title={endpointMetadata?.endpointOwnerTeam} style={{ textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }} onClick={() => copyToClipboard(endpointMetadata?.endpointOwnerTeam!)}>
          {endpointMetadata?.endpointOwnerTeam!}
        </GridItem>

        <Text className="Header">Owner (PO)</Text>
        <GridItem colSpan={2} className="Cell" title={endpointMetadata?.endpointOwner} style={{ textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }} onClick={() => copyToClipboard(endpointMetadata?.endpointOwner!)}>
          {endpointMetadata?.endpointOwner!}
        </GridItem>

        <Text className="Header">Owner email (PO)</Text>
        <GridItem colSpan={2} className="Cell" title={endpointMetadata?.endpointOwnerEmail} style={{ textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }} onClick={() => copyToClipboard(endpointMetadata?.endpointOwnerEmail!)}>
          {endpointMetadata?.endpointOwnerEmail!}
        </GridItem>
      </Grid>
      <br />
      <Divider />
      {hasTechnicalContact ? <TechnicalTable /> : <Text>No technical contact</Text>}
      <Text as="i" color={feedbackColour}>
        {feedback}
      </Text>
      <br />

      <MetadataButton onClose={setendpointMetadata} metaData={endpointMetadata} icon={<EditIcon />} buttonText="Update metadata" />
    </Box>
  );

  const TechnicalTable = () => (
    <SimpleGrid columns={2}>
      <GridItem colSpan={2} textAlign="center" border={"0px"} textColor="black" className="Header" key={"techTitle"}>
        Technical Contacts
      </GridItem>

      <Text key={"nameHeader"} className="Header">
        Name
      </Text>
      <Text key={"emailHeader"} className="Header">
        Email
      </Text>

      {endpointMetadata?.technicalContacts?.map((technicalContact, idx) => (
        <GridItem colSpan={2} key={idx + "gridItem"}>
          <SimpleGrid columns={2} key={idx + "grid"}>
            <Text w="100%" title={technicalContact.name} key={idx + "namecol"} style={{ textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }} onClick={() => copyToClipboard(technicalContact.name!)} className="Cell">
              {technicalContact.name}
            </Text>

            <Text w="100%" title={technicalContact.email} key={idx + "emailcol"} style={{ textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }} onClick={() => copyToClipboard(technicalContact.email!)} className="Cell">
              {technicalContact.email}
            </Text>
          </SimpleGrid>
        </GridItem>
      ))}
    </SimpleGrid>
  );

  function copyToClipboard(textToCopy: string) {
    navigator.clipboard.writeText(textToCopy!);
    setFeedback("Copied " + textToCopy + " to clipboard");
    clearTimeout(timeoutIdRef.current);
    timeoutIdRef.current = setTimeout(() => {
      clearAlertsFeedback();
    }, 4000);
  }

  const NoMetaData = () => (
    <Box padding="3">
      <Text>There is no information available click add to metadata endpoint</Text>
      <MetadataButton onClose={setendpointMetadata} metaData={endpointMetadata} icon={<AddIcon />} buttonText="Add metadata" />
    </Box>
  );

  return !hasMetaData ? <NoMetaData /> : <HasMetaData />;
};
export default MetadataColumn;
