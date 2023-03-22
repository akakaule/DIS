/* @jsx jsx */
import * as React from "react";
import { Flex, Badge } from "@chakra-ui/react";
import { jsx } from "@emotion/react";
import { getPlatformVersion } from "hooks/app-status";


type FooterProps = {};

const Footer = (props: FooterProps) => {

  const [platformVersion, setPlatformVersion] = React.useState(getPlatformVersion());
  return (
    <Flex
      flexDirection="row"
      justifyContent="space-between"
      wrap="nowrap"
      margin="2rem 10% 1rem"
      borderTop="1px solid #dee2e6"
      boxSizing="border-box"

      fontSize="1rem"
      textAlign="left"
      {...props}
    >
      <Badge color="grey.900">{platformVersion !== undefined ? platformVersion : getPlatformVersion()}</Badge>
      <Badge color="grey.900">&copy; 2023 - Delegate</Badge>


    </Flex>
  );
};

export default Footer;
