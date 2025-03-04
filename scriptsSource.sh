#!/bin/bash


#####################################################
## HEADERS/MESSAGING
#####################################################

DIV=$(printf '%.0s-' {1..50});

createHeader(){
    echo "";
    echo "";
    echo -e "\e[32m$DIV";
    echo -e "\e[32m-- $1";
    echo -e "\e[32m$DIV";
    echo -e "\e[37m";
    echo "";
}


createSubsection(){
    echo "";
    echo "";
    echo -e "\e[34m    -- $1";
    echo -e "\e[34m    $DIV";
    echo -e "\e[37m";
    echo "";
}


createSuccess(){
    echo "";
    echo "";
    echo -e "\e[35m-- $1";
    echo -e "\e[35m$DIV";
    echo -e "\e[37m";
    echo "";
}


createError(){
    local ERR_MSG="$2";
    if [[-z "$ERR_MSG" ]]; then
        ERR_MSG="An Unknown Error Has Occurred";
    fi;

    echo "";
    echo "";
    echo -e "\e[31m-- $ERR_MSG";
    echo -e "\e[31m-- ErrCode: $1";
    echo -e "\e[31m$DIV";
    echo -e "\e[37m";
    echo "";
}


createResult(){
    local RESULT_CODE="$1";
    local ERROR_MSG="$2";
    local SUCCESS_MSG="$3";

    if [[ -z "$2" ]]; then
        ERROR_MSG="Error: Unknown";
    fi;
    if [[ -z "$3" ]]; then  
        SUCCESS_MSG="SUCCESS";
    fi;

    if [[ "$RESULT_CODE" -gt 0 ]]; then 
            createError "$RESULT_CODE" "FAILED: $ERROR_MSG";
        else
            createSuccess "SUCCESS: $SUCCESS_MSG";
    fi;

    return "$RESULT_CODE";
}


createNote(){
    echo "";
    echo "";
    echo -e "\e[34m       $1";
    echo -e "\e[34m       $DIV";
    echo -e "\e[37m";
    echo "";
}

copyMessage(){
    echo "";
    echo "";
    echo -e "\e[34m    -- Copying =>";
    echo -e "\e[34m    -- Source: $1";
    echo -e "\e[34m    -- Dest:   $2";
    echo -e "\e[34m    $DIV";
    echo -e "\e[37m";
    echo "";
}


#####################################################
## DEPLOYMENTS
#####################################################


deployScriptFiles(){
    createHeader "Copying Files to Vegas";
    find ./Scripts -type f -name "*.cs" | while read file; do
        find "/c/Program Files/VEGAS/" -type d -name "Script Menu" | while read targetDir; do
            copyMessage "$file" "$targetDir";
            cp "$file" "$targetDir/"
        done;
    done;
}



#####################################################
## GIT FUNCTIONS
#####################################################

gitPushAll(){
    createHeader "Starting Push";
    git add .; 
    git commit -m "$1" && git push;
}

