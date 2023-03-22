import { Box, SimpleGrid } from "@chakra-ui/react";
import * as api from "api-client";
import * as React from "react";
import { useParams } from "react-router-dom";
import HeartbeatColumn from "../metadata/heartbeat-column";
import MetadataColumn from "../metadata/metadata-column";

interface IMetadataTabProps {}

const MetadataTab = (props: IMetadataTabProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();

  const [fetchDone, setFetchDone] = React.useState<boolean>(false);
  const [endpointMetadata, setendpointMetadata] = React.useState<api.Metadata>();

  React.useEffect(() => {
    const fetchData = async () => {
      try {
        const res = await client.getMetadataEndpoint(params.id!);
        setendpointMetadata(res);
        setFetchDone(true);
      } catch (error) {
        setFetchDone(true);
      }
    };

    fetchData();
  }, []);

  return (
    <SimpleGrid columns={2} spacing={6} width="100%">
      <Box key={"metadata"} w="100%" border={"1px"} borderColor={" var(--chakra-colors-chakra-border-color)"} borderRadius={"4px"}>
        {fetchDone && <MetadataColumn metadata={endpointMetadata} />}
      </Box>
      <Box key={"heartbeat"} w="100%" border={"1px"} borderColor={" var(--chakra-colors-chakra-border-color)"} padding={3} borderRadius={"4px"}>
        {fetchDone && <HeartbeatColumn metadata={endpointMetadata} />}
      </Box>
    </SimpleGrid>
  );
};

export default MetadataTab;
