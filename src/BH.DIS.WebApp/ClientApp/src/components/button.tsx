import { Button as ChakraButton, ButtonProps} from "@chakra-ui/react"
import React from "react"
import { delegateBlue } from "shared-styles/colors"

interface IButton extends ButtonProps {
    onClick: (event: React.MouseEvent<any, MouseEvent>) => void
}

const Button: React.FunctionComponent<IButton> = (props) => {
    
    const getBackgroundColor = (): string => {
        if (props.variant === undefined || props.variant === 'solid') {
            return delegateBlue;
        }
        return '';
    }

    return (
        <ChakraButton {...props} isDisabled={props.isDisabled} background={getBackgroundColor()} colorScheme="blue" size="xs">
            {props.children}
        </ChakraButton>
    )

}

export default Button