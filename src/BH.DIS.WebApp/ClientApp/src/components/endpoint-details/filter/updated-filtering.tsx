import * as React from "react";
import FilterContext from "./filtering-context";
import {TextField } from '@material-ui/core';
import { createStyles, makeStyles, Theme } from '@material-ui/core/styles';
import moment from "moment";

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        container: {
            display: 'flex',
            flexWrap: 'wrap',
        },
        textField: {
            marginLeft: theme.spacing(1),
            marginRight: theme.spacing(1),
            width: '-webkit-fill-available',
            paddingBottom: '0.5rem'
        },
    }),
);

type UpdatedFilteringProps = {

}

const UpdatedFiltering = (props: UpdatedFilteringProps) => {
    const classes = useStyles();

    const ctx = React.useContext(FilterContext);
    const [updatedTo, setUpdatedTo] = React.useState<string>();
    const [updatedFrom, setUpdatedFrom] = React.useState<string>();


    React.useEffect(() => {
        const filters = ctx.filterContext;
        filters.updatedAtTo = moment(updatedTo, 'YYYY-MM-DD[T]HH:mm:ss');
        filters.updateAtFrom = moment(updatedFrom, 'YYYY-MM-DD[T]HH:mm:ss');
        ctx.setProjectContext(filters);
    }, [updatedTo, updatedFrom]);

    return (
        <form className={classes.container} noValidate={true}>
            <TextField
                id="datetime-local"
                label="Added From"
                type="datetime-local"
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                    setUpdatedFrom(event.target.value);
                }}
                className={classes.textField}
                InputLabelProps={{
                    shrink: true,
                }}
            />
            <TextField
                id="datetime-local"
                label="Added To"
                type="datetime-local"
                className={classes.textField}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                    setUpdatedTo(event.target.value);
                }}
                InputLabelProps={{
                    shrink: true,
                }}
            />
        </form>
        )
}

export default UpdatedFiltering;